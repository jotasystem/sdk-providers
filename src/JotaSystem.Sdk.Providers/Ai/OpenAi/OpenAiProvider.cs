using JotaSystem.Sdk.Providers.Ai.OpenAi.Models;
using OpenAI.Chat;

namespace JotaSystem.Sdk.Providers.Ai.OpenAi
{
    public class OpenAiProvider(ChatClient chatClient) : IOpenAiProvider
    {
        private readonly ChatClient _chatClient = chatClient;

        public async Task<OpenAiChatResponse> ChatAsync(OpenAiChatRequest request, CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage>();

            foreach (var message in request.Messages)
            {
                messages.Add(message.Role switch
                {
                    "system" => ChatMessage.CreateSystemMessage(message.Content),
                    "assistant" => ChatMessage.CreateAssistantMessage(message.Content),
                    _ => ChatMessage.CreateUserMessage(message.Content)
                });
            }

            var options = new ChatCompletionOptions
            {
                Temperature = request.Temperature,
                MaxOutputTokenCount = request.MaxTokens
            };

            ChatCompletion completion =
                await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            var content = string.Concat(
                completion.Content
                    .Where(c => c.Kind == ChatMessageContentPartKind.Text)
                    .Select(c => c.Text)
            );

            return new OpenAiChatResponse
            {
                Content = content,
                PromptTokens = completion.Usage?.InputTokenCount ?? 0,
                CompletionTokens = completion.Usage?.OutputTokenCount ?? 0
            };
        }
    }
}
