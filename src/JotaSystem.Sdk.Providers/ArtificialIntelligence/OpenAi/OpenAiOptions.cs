namespace JotaSystem.Sdk.Providers.ArtificialIntelligence.OpenAi
{
    public class OpenAiOptions
    {
        public string ApiKey { get; set; } = default!;
        public string Model { get; set; } = "gpt-4o-mini";
    }
}