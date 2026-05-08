namespace TokenGuard.Core.Entities;

public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal DailyLimitUsd { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UsageRecord> UsageRecords { get; set; } = new List<UsageRecord>();
}
