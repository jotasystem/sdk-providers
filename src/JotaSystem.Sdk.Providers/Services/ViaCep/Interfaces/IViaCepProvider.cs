using JotaSystem.Sdk.Providers.Common;
using JotaSystem.Sdk.Providers.Services.ViaCep.Models;

namespace JotaSystem.Sdk.Providers.Services.ViaCep.Interfaces
{
    public interface IViaCepProvider
    {
        public interface IViaCepProvider
        {
            /// <summary>
            /// Consulta informações de endereço a partir de um CEP.
            /// </summary>
            Task<ApiResponse<ViaCepResponse>> GetAddressAsync(string cep);
        }
    }
}
