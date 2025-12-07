using ViaCep;

namespace JotaSystem.Sdk.Providers.Address.ViaCep
{
    /// <summary>
    /// https://viacep.com.br/
    /// </summary>
    public interface IViaCepProvider
    {
        /// <summary>
        /// Consulta informações de endereço a partir de um CEP.
        /// </summary>
        Task<ViaCepResult?> GetAddressByCepAsync(string cep, CancellationToken cancellationToken = default);
    }
}