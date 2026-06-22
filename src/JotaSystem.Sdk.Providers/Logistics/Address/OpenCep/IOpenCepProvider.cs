using JotaSystem.Sdk.Providers.Logistics.Address.OpenCep.Models;

namespace JotaSystem.Sdk.Providers.Logistics.Address.OpenCep
{
    /// <summary>
    /// https://opencep.com/
    /// </summary>
    public interface IOpenCepProvider
    {
        /// <summary>
        /// Consulta informações de endereço a partir de um CEP.
        /// </summary>
        Task<ApiResponse<OpenCepResponse>> GetAddressByCepAsync(string cep);
    }
}
