using JotaSystem.Sdk.Common.Extensions.String;
using ViaCep;

namespace JotaSystem.Sdk.Providers.Logistics.Address.ViaCep
{
    public class ViaCepProvider(IViaCepClient client) : IViaCepProvider
    {
        private readonly IViaCepClient _client = client;

        public async Task<ViaCepResult?> GetAddressByCepAsync(string cep, CancellationToken cancellationToken = default)
        {
            var sanitizedCep = cep.NormalizeSpecialCharacter();

            var result = await _client.SearchAsync(sanitizedCep, cancellationToken);
            return result ?? null;
        }

        public async Task<IEnumerable<ViaCepResult>> GetCepsByAddressAsync(string state, string city, string street, CancellationToken cancellationToken = default)
        {
            var sanitizedState = state?.Trim().ToUpperInvariant();
            var sanitizedCity = city?.NormalizeWhitespace();
            var sanitizedStreet = street?.NormalizeWhitespace();

            if (sanitizedState is null || sanitizedState.Length != 2 || !sanitizedState.All(char.IsLetter))
                throw new ArgumentException("UF deve conter exatamente duas letras.", nameof(state));

            if (string.IsNullOrWhiteSpace(sanitizedCity) || sanitizedCity.Length < 3)
                throw new ArgumentException($"Cidade deve conter ao menos três caracteres.", nameof(city));

            if (string.IsNullOrWhiteSpace(sanitizedStreet) || sanitizedStreet.Length < 3)
                throw new ArgumentException($"Logradouro deve conter ao menos três caracteres.", nameof(street));

            var results = await _client.SearchAsync(sanitizedState, sanitizedCity, sanitizedStreet, cancellationToken);
            return results ?? [];
        }
    }
}
