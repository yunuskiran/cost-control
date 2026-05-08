using TokenGuard.Core.Entities;

namespace TokenGuard.Core.Interfaces;

public interface IUsageRepository
{
    Task<ApiKey?> GetApiKeyByHashAsync(string keyHash, CancellationToken ct = default);
    Task<ApiKey?> GetApiKeyByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ApiKey>> GetAllApiKeysAsync(CancellationToken ct = default);
    Task AddApiKeyAsync(ApiKey apiKey, CancellationToken ct = default);
    Task UpdateApiKeyAsync(ApiKey apiKey, CancellationToken ct = default);
    Task AddUsageRecordAsync(UsageRecord record, CancellationToken ct = default);
    Task<IEnumerable<UsageRecord>> GetUsageByApiKeyAsync(Guid apiKeyId, int days, CancellationToken ct = default);
    Task<IEnumerable<UsageRecord>> GetAllUsageTodayAsync(CancellationToken ct = default);
}
