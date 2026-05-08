using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TokenGuard.Api.Configuration;
using TokenGuard.Core.Interfaces;

namespace TokenGuard.Api.Middleware;

public class ProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProxyMiddleware> _logger;

    public ProxyMiddleware(RequestDelegate next, ILogger<ProxyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IUsageRepository usageRepo,
        IBudgetService budgetService,
        ITokenCountingService tokenCounter,
        IOptions<ProvidersOptions> providers,
        IHttpClientFactory httpClientFactory)
    {
        // Extract provider from path: /v1/proxy/{provider}/...
        var path = context.Request.Path.Value ?? "";
        var segments = path.TrimStart('/').Split('/', 3);
        if (segments.Length < 3 || segments[0] != "v1" || segments[1] != "proxy")
        {
            await _next(context);
            return;
        }

        var providerName = segments[2].Split('/')[0];
        var remainingPath = segments[2].Contains('/')
            ? "/" + segments[2][(segments[2].IndexOf('/') + 1)..]
            : "/";

        // Authenticate via X-TokenGuard-Key header
        if (!context.Request.Headers.TryGetValue("X-TokenGuard-Key", out var rawKey) || string.IsNullOrWhiteSpace(rawKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "missing_api_key" });
            return;
        }

        var keyHash = ComputeSha256Hash(rawKey.ToString());
        var apiKey = await usageRepo.GetApiKeyByHashAsync(keyHash);
        if (apiKey is null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "invalid_api_key" });
            return;
        }

        // Check budget
        var budgetStatus = await budgetService.GetBudgetStatusAsync(apiKey.Id, apiKey.DailyLimitUsd);
        if (budgetStatus.IsBlocked)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "budget_exceeded",
                spent = budgetStatus.SpentTodayUsd,
                limit = budgetStatus.LimitUsd
            });
            return;
        }

        // Determine failover order
        var providerOrder = BuildFailoverList(providerName, providers.Value);
        HttpResponseMessage? upstreamResponse = null;
        string? usedProvider = null;

        foreach (var pName in providerOrder)
        {
            var providerConfig = providers.Value.GetProvider(pName);
            if (providerConfig is null) continue;

            var upstreamApiKey = Environment.GetEnvironmentVariable(providerConfig.ApiKeyEnvVar);
            if (string.IsNullOrWhiteSpace(upstreamApiKey))
            {
                _logger.LogWarning("Upstream API key env var {EnvVar} is not set for provider {Provider}", providerConfig.ApiKeyEnvVar, pName);
                continue;
            }

            try
            {
                var client = httpClientFactory.CreateClient("upstream");
                var targetUrl = providerConfig.BaseUrl.TrimEnd('/') + remainingPath;
                if (context.Request.QueryString.HasValue)
                    targetUrl += context.Request.QueryString.Value;

                context.Request.EnableBuffering();
                var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                var upstreamRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl)
                {
                    Content = string.IsNullOrEmpty(requestBody)
                        ? null
                        : new StringContent(requestBody, Encoding.UTF8, context.Request.ContentType ?? "application/json")
                };

                // Forward relevant headers (skip host/content-length)
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.StartsWith("X-TokenGuard", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!upstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                        upstreamRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }

                // Inject upstream API key
                upstreamRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", upstreamApiKey);

                upstreamResponse = await client.SendAsync(upstreamRequest, HttpCompletionOption.ResponseContentRead);

                if ((int)upstreamResponse.StatusCode >= 500)
                {
                    _logger.LogWarning("Provider {Provider} returned {Status}, trying failover", pName, upstreamResponse.StatusCode);
                    upstreamResponse.Dispose();
                    upstreamResponse = null;
                    continue;
                }

                usedProvider = pName;
                break;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} request failed, trying failover", pName);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} timed out, trying failover", pName);
            }
        }

        if (upstreamResponse is null)
        {
            context.Response.StatusCode = 502;
            await context.Response.WriteAsJsonAsync(new { error = "all_providers_failed" });
            return;
        }

        using (upstreamResponse)
        {
            var responseBody = await upstreamResponse.Content.ReadAsStringAsync();

            context.Response.StatusCode = (int)upstreamResponse.StatusCode;
            foreach (var header in upstreamResponse.Headers)
            {
                if (string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            var contentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "application/json";
            context.Response.ContentType = contentType;

            if (upstreamResponse.IsSuccessStatusCode && usedProvider is not null)
            {
                // Parse token usage from response
                var (inputTokens, outputTokens, modelName) = ExtractUsage(responseBody, usedProvider);
                var cost = tokenCounter.EstimateCostUsd(usedProvider, modelName, inputTokens, outputTokens);

                await budgetService.RecordUsageAsync(apiKey.Id, cost);
                await usageRepo.AddUsageRecordAsync(new Core.Entities.UsageRecord
                {
                    ApiKeyId = apiKey.Id,
                    Provider = usedProvider,
                    ModelName = modelName,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    EstimatedCostUsd = cost
                });
            }

            await context.Response.WriteAsync(responseBody);
        }
    }

    private static (int input, int output, string model) ExtractUsage(string responseBody, string provider)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            var model = root.TryGetProperty("model", out var m) ? m.GetString() ?? "unknown" : "unknown";

            // OpenAI / Gemini (openai-compatible) format
            if (root.TryGetProperty("usage", out var usage))
            {
                var input = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32()
                          : usage.TryGetProperty("input_tokens", out var it) ? it.GetInt32() : 0;
                var output = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32()
                           : usage.TryGetProperty("output_tokens", out var ot) ? ot.GetInt32() : 0;
                return (input, output, model);
            }

            // Anthropic format
            if (root.TryGetProperty("input_tokens", out var ait) && root.TryGetProperty("output_tokens", out var aot))
                return (ait.GetInt32(), aot.GetInt32(), model);
        }
        catch { /* best-effort */ }

        return (0, 0, "unknown");
    }

    private static List<string> BuildFailoverList(string primaryProvider, ProvidersOptions options)
    {
        var all = new[] { "openai", "gemini", "anthropic" };
        var ordered = new List<string> { primaryProvider.ToLowerInvariant() };
        ordered.AddRange(all.Where(p => !p.Equals(primaryProvider, StringComparison.OrdinalIgnoreCase)));
        return ordered;
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
