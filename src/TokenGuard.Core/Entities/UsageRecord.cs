namespace TokenGuard.Core.Entities;

public class UsageRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApiKeyId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiKey? ApiKey { get; set; }
}
