namespace JotaSystem.Sdk.Providers.Ai.OpenAi.Models
{
    public class OpenAiChatRequest
    {
        public List<AiMessage> Messages { get; set; } = [];
        public float Temperature { get; set; } = (float)0.7;
        public int MaxTokens { get; set; } = 1000;
    }

    public class AiMessage
    {
        public string Role { get; set; } = default!; // system | user | assistant
        public string Content { get; set; } = default!;
    }
}