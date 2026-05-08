using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TokenGuard.Core.Interfaces;

namespace TokenGuard.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IUsageRepository _repo;
    private readonly IBudgetService _budget;

    public DashboardController(IUsageRepository repo, IBudgetService budget)
    {
        _repo = repo;
        _budget = budget;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var todayRecords = await _repo.GetAllUsageTodayAsync(ct);
        var recordsList = todayRecords.ToList();

        var totalSpendToday = recordsList.Sum(r => r.EstimatedCostUsd);

        var perProvider = recordsList
            .GroupBy(r => r.Provider)
            .Select(g => new
            {
                provider = g.Key,
                totalCost = g.Sum(r => r.EstimatedCostUsd),
                totalInputTokens = g.Sum(r => r.InputTokens),
                totalOutputTokens = g.Sum(r => r.OutputTokens),
                requestCount = g.Count()
            })
            .ToList();

        // Top consumers by API key
        var allKeys = await _repo.GetAllApiKeysAsync(ct);
        var keyLookup = allKeys.ToDictionary(k => k.Id, k => k.Name);

        var topConsumers = recordsList
            .GroupBy(r => r.ApiKeyId)
            .Select(g => new
            {
                apiKeyId = g.Key,
                apiKeyName = keyLookup.TryGetValue(g.Key, out var n) ? n : "unknown",
                totalCost = g.Sum(r => r.EstimatedCostUsd),
                requestCount = g.Count()
            })
            .OrderByDescending(x => x.totalCost)
            .Take(10)
            .ToList();

        // Spend over last 7 days
        var last7Days = await GetSpendLast7DaysAsync(ct);

        return Ok(new
        {
            totalSpendTodayUsd = totalSpendToday,
            perProvider,
            topConsumers,
            spendLast7Days = last7Days
        });
    }

    private async Task<List<object>> GetSpendLast7DaysAsync(CancellationToken ct)
    {
        var result = new List<object>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var nextDate = date.AddDays(1);

            var allKeys = await _repo.GetAllApiKeysAsync(ct);
            decimal dayTotal = 0;

            foreach (var key in allKeys)
            {
                var records = await _repo.GetUsageByApiKeyAsync(key.Id, 7, ct);
                dayTotal += records
                    .Where(r => r.Timestamp >= date && r.Timestamp < nextDate)
                    .Sum(r => r.EstimatedCostUsd);
            }

            result.Add(new { date = date.ToString("yyyy-MM-dd"), spendUsd = dayTotal });
        }

        return result;
    }
}
