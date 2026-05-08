namespace TokenGuard.Core.Models;

public class BudgetStatus
{
    public Guid ApiKeyId { get; set; }
    public decimal SpentTodayUsd { get; set; }
    public decimal LimitUsd { get; set; }
    public bool IsBlocked => SpentTodayUsd >= LimitUsd;
}
