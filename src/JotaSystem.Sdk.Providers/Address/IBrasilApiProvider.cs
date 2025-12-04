using JotaSystem.Sdk.Providers.Address.BrasilApi.Models;
using JotaSystem.Sdk.Providers.Common;

namespace JotaSystem.Sdk.Providers.Address
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