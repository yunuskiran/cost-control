using Microsoft.EntityFrameworkCore;
using TokenGuard.Core.Entities;
using TokenGuard.Core.Interfaces;
using TokenGuard.Infrastructure.Data;

namespace TokenGuard.Infrastructure.Repositories;

public class UsageRepository : IUsageRepository
{
    private readonly AppDbContext _db;

    public UsageRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiKey?> GetApiKeyByHashAsync(string keyHash, CancellationToken ct = default)
        => await _db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive, ct);

    public async Task<ApiKey?> GetApiKeyByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ApiKeys.FindAsync(new object[] { id }, ct);

    public async Task<IEnumerable<ApiKey>> GetAllApiKeysAsync(CancellationToken ct = default)
        => await _db.ApiKeys.OrderBy(k => k.CreatedAt).ToListAsync(ct);

    public async Task AddApiKeyAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        await _db.ApiKeys.AddAsync(apiKey, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateApiKeyAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        _db.ApiKeys.Update(apiKey);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddUsageRecordAsync(UsageRecord record, CancellationToken ct = default)
    {
        await _db.UsageRecords.AddAsync(record, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<UsageRecord>> GetUsageByApiKeyAsync(Guid apiKeyId, int days, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await _db.UsageRecords
            .Where(r => r.ApiKeyId == apiKeyId && r.Timestamp >= since)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<UsageRecord>> GetAllUsageTodayAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.UsageRecords
            .Where(r => r.Timestamp >= today)
            .ToListAsync(ct);
    }
}
