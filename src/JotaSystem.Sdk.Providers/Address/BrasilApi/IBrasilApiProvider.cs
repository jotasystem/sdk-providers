using JotaSystem.Sdk.Providers.Address.BrasilApi.Models;

namespace JotaSystem.Sdk.Providers.Address.BrasilApi
{
    /// <summary>
    /// https://brasilapi.com.br/
    /// </summary>
    public interface IBrasilApiProvider
    {
        Task<ApiResponse<List<BrasilApiBankResponse>>> GetBanksAsync();
        Task<ApiResponse<BrasilApiCepResponse>> GetCepAsync(string cep);
    }
}