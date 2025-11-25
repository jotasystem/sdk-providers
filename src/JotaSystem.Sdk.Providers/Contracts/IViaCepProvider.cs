using JotaSystem.Sdk.Providers.Common;
using JotaSystem.Sdk.Providers.Services.ViaCep.Models;

namespace JotaSystem.Sdk.Providers.Contracts
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