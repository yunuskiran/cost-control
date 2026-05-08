using TokenGuard.Core.Models;

namespace TokenGuard.Core.Interfaces;

public interface IBudgetService
{
    Task<BudgetStatus> GetBudgetStatusAsync(Guid apiKeyId, decimal limitUsd, CancellationToken ct = default);
    Task<bool> IsBlockedAsync(Guid apiKeyId, decimal limitUsd, CancellationToken ct = default);
    Task RecordUsageAsync(Guid apiKeyId, decimal costUsd, CancellationToken ct = default);
    Task<decimal> GetSpentTodayAsync(Guid apiKeyId, CancellationToken ct = default);
}
