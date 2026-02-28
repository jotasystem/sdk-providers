using JotaSystem.Sdk.Common.Extensions.String;
using ViaCep;

namespace JotaSystem.Sdk.Providers.Address.ViaCep
{
    public class ViaCepProvider(HttpClient httpClient) : IViaCepProvider
    {
        private readonly ViaCepClient _client = new(httpClient);

        public async Task<ViaCepResult?> GetAddressByCepAsync(string cep,CancellationToken cancellationToken = default)
        {
            var sanitizedCep = cep.NormalizeSpecialCharacter();

            var result = await _client.SearchAsync(sanitizedCep, cancellationToken);

            if (result is null)
                return null;

            return result;
        }
    }
}