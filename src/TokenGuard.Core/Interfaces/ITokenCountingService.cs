namespace TokenGuard.Core.Interfaces;

public interface ITokenCountingService
{
    int EstimateTokenCount(string text);
    decimal EstimateCostUsd(string provider, string modelName, int inputTokens, int outputTokens);
}
