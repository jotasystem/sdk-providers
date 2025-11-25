using JotaSystem.Sdk.Providers.Common;
using JotaSystem.Sdk.Providers.Services.BrasilApi.Models;

namespace JotaSystem.Sdk.Providers.Contracts
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