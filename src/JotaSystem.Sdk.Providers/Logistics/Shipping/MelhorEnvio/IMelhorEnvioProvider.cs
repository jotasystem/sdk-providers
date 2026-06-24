using JotaSystem.Sdk.Providers.Logistics.Shipping.MelhorEnvio.Models;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.MelhorEnvio
{
    public interface IMelhorEnvioProvider
    {
        Task<ApiResponse<List<MelhorEnvioShippingQuoteResponse>>> CalculateShippingAsync(
            MelhorEnvioShippingQuoteRequest request,
            string? accessToken = null,
            CancellationToken cancellationToken = default);
    }
}
