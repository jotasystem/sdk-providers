using JotaSystem.Sdk.Providers.Ai.OpenAi;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace JotaSystem.Sdk.Providers.Ai
{
    public static class AiExtensions
    {
        public static JotaSystemSdkBuilder AddOpenAi(this JotaSystemSdkBuilder builder, Action<OpenAiOptions> configure)
        {
            var options = new OpenAiOptions();
            configure(options);

            builder.Services.AddSingleton(sp =>
            {
                var apiKey = options.ApiKey;
                var client = new OpenAIClient(apiKey);

                return client.GetChatClient(options.Model);
            });

            builder.Services.AddScoped<IOpenAiProvider, OpenAiProvider>();

            return builder;
        }
    }
}