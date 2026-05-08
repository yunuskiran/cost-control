using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TokenGuard.Core.Entities;
using TokenGuard.Core.Interfaces;

namespace TokenGuard.Api.Controllers;

[ApiController]
[Route("api/keys")]
[Authorize]
public class KeysController : ControllerBase
{
    private readonly IUsageRepository _repo;
    private readonly IBudgetService _budget;

    public KeysController(IUsageRepository repo, IBudgetService budget)
    {
        _repo = repo;
        _budget = budget;
    }

    [HttpPost]
    public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request, CancellationToken ct)
    {
        var plainKey = GenerateApiKey();
        var hash = ComputeSha256Hash(plainKey);

        var apiKey = new ApiKey
        {
            Name = request.Name,
            KeyHash = hash,
            ProjectName = request.ProjectName,
            DailyLimitUsd = request.DailyLimitUsd,
            IsActive = true
        };

        await _repo.AddApiKeyAsync(apiKey, ct);

        return Ok(new
        {
            id = apiKey.Id,
            name = apiKey.Name,
            projectName = apiKey.ProjectName,
            dailyLimitUsd = apiKey.DailyLimitUsd,
            createdAt = apiKey.CreatedAt,
            key = plainKey  // returned only once
        });
    }

    [HttpGet]
    public async Task<IActionResult> ListKeys(CancellationToken ct)
    {
        var keys = await _repo.GetAllApiKeysAsync(ct);
        var result = new List<object>();

        foreach (var k in keys)
        {
            var spent = await _budget.GetSpentTodayAsync(k.Id, ct);
            result.Add(new
            {
                id = k.Id,
                name = k.Name,
                projectName = k.ProjectName,
                dailyLimitUsd = k.DailyLimitUsd,
                isActive = k.IsActive,
                createdAt = k.CreatedAt,
                spentTodayUsd = spent,
                isBlocked = spent >= k.DailyLimitUsd
            });
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateKey(Guid id, CancellationToken ct)
    {
        var key = await _repo.GetApiKeyByIdAsync(id, ct);
        if (key is null) return NotFound();

        key.IsActive = false;
        await _repo.UpdateApiKeyAsync(key, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/usage")]
    public async Task<IActionResult> GetUsage(Guid id, [FromQuery] int days = 7, CancellationToken ct = default)
    {
        var key = await _repo.GetApiKeyByIdAsync(id, ct);
        if (key is null) return NotFound();

        var records = await _repo.GetUsageByApiKeyAsync(id, days, ct);
        return Ok(records.Select(r => new
        {
            r.Id,
            r.Provider,
            r.ModelName,
            r.InputTokens,
            r.OutputTokens,
            r.EstimatedCostUsd,
            r.Timestamp
        }));
    }

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return "tg_" + Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}

public record CreateKeyRequest(string Name, string ProjectName, decimal DailyLimitUsd);
