using StackExchange.Redis;
using TokenGuard.Core.Interfaces;
using TokenGuard.Core.Models;

namespace TokenGuard.Infrastructure.Services;

public class RedisBudgetService : IBudgetService
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan TtL = TimeSpan.FromHours(48);

    public RedisBudgetService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private static string SpendKey(Guid apiKeyId) =>
        $"DailySpend:{apiKeyId}:{DateTime.UtcNow:yyyy-MM-dd}";

    public async Task<decimal> GetSpentTodayAsync(Guid apiKeyId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(SpendKey(apiKeyId));
        return value.HasValue && decimal.TryParse(value.ToString(), out var spent) ? spent : 0m;
    }

    public async Task<BudgetStatus> GetBudgetStatusAsync(Guid apiKeyId, decimal limitUsd, CancellationToken ct = default)
    {
        var spent = await GetSpentTodayAsync(apiKeyId, ct);
        return new BudgetStatus
        {
            ApiKeyId = apiKeyId,
            SpentTodayUsd = spent,
            LimitUsd = limitUsd
        };
    }

    public async Task<bool> IsBlockedAsync(Guid apiKeyId, decimal limitUsd, CancellationToken ct = default)
    {
        var spent = await GetSpentTodayAsync(apiKeyId, ct);
        return spent >= limitUsd;
    }

    public async Task RecordUsageAsync(Guid apiKeyId, decimal costUsd, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = SpendKey(apiKeyId);
        await db.StringIncrementAsync(key, (double)costUsd);
        await db.KeyExpireAsync(key, TtL);
    }
}
