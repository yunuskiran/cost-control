namespace TokenGuard.Api.Configuration;

public class ProviderConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKeyEnvVar { get; set; } = string.Empty;
}

public class ProvidersOptions
{
    public const string Section = "Providers";
    public ProviderConfig OpenAI { get; set; } = new();
    public ProviderConfig Gemini { get; set; } = new();
    public ProviderConfig Anthropic { get; set; } = new();

    public ProviderConfig? GetProvider(string name) => name.ToLowerInvariant() switch
    {
        "openai"    => OpenAI,
        "gemini"    => Gemini,
        "anthropic" => Anthropic,
        _ => null
    };
}
