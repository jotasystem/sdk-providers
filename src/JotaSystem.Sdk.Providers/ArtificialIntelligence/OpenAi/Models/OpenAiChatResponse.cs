namespace JotaSystem.Sdk.Providers.Ai.OpenAi.Models
{
    public class OpenAiChatResponse
    {
        public string Content { get; set; } = default!;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public string? RawResponse { get; set; } // útil para debug/log
    }
}