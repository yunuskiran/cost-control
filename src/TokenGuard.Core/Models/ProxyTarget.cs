namespace TokenGuard.Core.Models;

public enum ProviderType
{
    OpenAI,
    Gemini,
    Anthropic
}

public class ProxyTarget
{
    public ProviderType Provider { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKeyEnvVar { get; set; } = string.Empty;
}
