using TokenGuard.Core.Interfaces;

namespace TokenGuard.Infrastructure.Services;

public class SimpleTokenCountingService : ITokenCountingService
{
    // Cost per token in USD: (inputCost, outputCost)
    private static readonly Dictionary<string, (decimal Input, decimal Output)> Rates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["openai:gpt-4o"]            = (0.000005m,   0.000015m),
        ["openai:gpt-4o-mini"]       = (0.00000015m, 0.0000006m),
        ["openai:gpt-3.5-turbo"]     = (0.0000005m,  0.0000015m),
        ["gemini:gemini-1.5-flash"]  = (0.000000075m, 0.0000003m),
        ["gemini:gemini-1.5-pro"]    = (0.00000125m,  0.000005m),
        ["anthropic:claude-3-5-sonnet"] = (0.000003m, 0.000015m),
        ["anthropic:claude-3-haiku"] = (0.00000025m, 0.00000125m),
    };

    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)Math.Ceiling(wordCount * 1.3);
    }

    public decimal EstimateCostUsd(string provider, string modelName, int inputTokens, int outputTokens)
    {
        var key = $"{provider}:{modelName}";
        if (!Rates.TryGetValue(key, out var rates))
        {
            // Fallback to provider-level defaults
            rates = provider.ToLowerInvariant() switch
            {
                "openai"    => (0.000005m, 0.000015m),
                "gemini"    => (0.000000075m, 0.0000003m),
                "anthropic" => (0.000003m, 0.000015m),
                _           => (0.000005m, 0.000015m)
            };
        }

        return (inputTokens * rates.Input) + (outputTokens * rates.Output);
    }
}
