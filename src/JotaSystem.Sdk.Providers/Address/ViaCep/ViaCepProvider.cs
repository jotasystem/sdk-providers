using ViaCep;

namespace JotaSystem.Sdk.Providers.Address.ViaCep
{
    public class ViaCepProvider : IViaCepProvider
    {
        public async Task<ViaCepResult?> GetAddressByCepAsync(string cep, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cep))
                throw new ArgumentException("CEP inválido.");

            cep = new string([.. cep.Where(char.IsDigit)]);

            if (cep.Length != 8)
                throw new ArgumentException("CEP incompleto");

            var client = new ViaCepClient();
            return await client.SearchAsync(cep, cancellationToken);
        }
    }
}