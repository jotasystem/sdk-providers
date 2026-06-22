using ViaCep;

namespace JotaSystem.Sdk.Providers.Logistics.Address.ViaCep
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

        /// <summary>
        /// Pesquisa CEPs a partir da UF, cidade e logradouro.
        /// </summary>
        /// <remarks>
        /// A cidade e o logradouro devem possuir ao menos três caracteres.
        /// O ViaCEP retorna no máximo 50 resultados.
        /// </remarks>
        Task<IEnumerable<ViaCepResult>> GetCepsByAddressAsync(string state, string city, string street, CancellationToken cancellationToken = default);
    }
}
