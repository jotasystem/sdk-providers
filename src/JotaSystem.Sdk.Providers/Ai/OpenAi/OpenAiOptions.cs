namespace JotaSystem.Sdk.Providers.Ai.OpenAi
{
    public class OpenAiOptions
    {
        public string ApiKey { get; set; } = default!;
        public string Model { get; set; } = "gpt-4o-mini";
    }
}