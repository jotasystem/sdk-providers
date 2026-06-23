using JotaSystem.Sdk.Providers.Logistics.Address.BrasilApi.Models;

namespace JotaSystem.Sdk.Providers.Logistics.Address.BrasilApi
{
    /// <summary>
    /// https://brasilapi.com.br/
    /// </summary>
    public interface IBrasilApiProvider
    {
        Task<ApiResponse<List<BrasilApiBankResponse>>> GetBanksAsync();
        Task<ApiResponse<BrasilApiCepResponse>> GetAddressByCepAsync(string cep);
    }
}