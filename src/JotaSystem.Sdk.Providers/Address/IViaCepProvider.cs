using JotaSystem.Sdk.Providers.Address.ViaCep.Models;
using JotaSystem.Sdk.Providers.Common;

namespace JotaSystem.Sdk.Providers.Address
{
    /// <summary>
    /// https://viacep.com.br/
    /// </summary>
    public interface IViaCepProvider
    {
        /// <summary>
        /// Consulta informações de endereço a partir de um CEP.
        /// </summary>
        Task<ApiResponse<ViaCepResponse>> GetAddressAsync(string cep);
    }
}