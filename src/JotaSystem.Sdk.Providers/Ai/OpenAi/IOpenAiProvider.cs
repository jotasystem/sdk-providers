using JotaSystem.Sdk.Providers.Ai.OpenAi.Models;

namespace JotaSystem.Sdk.Providers.Ai.OpenAi
{
    public interface IOpenAiProvider
    {
        Task<OpenAiChatResponse> ChatAsync(OpenAiChatRequest request, CancellationToken cancellationToken = default);
    }
}