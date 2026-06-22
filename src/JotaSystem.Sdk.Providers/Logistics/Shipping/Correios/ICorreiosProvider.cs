using JotaSystem.Sdk.Providers.Logistics.Shipping.Correios.Models;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.Correios
{
    public interface ICorreiosProvider
    {
        Task<ApiResponse<List<CorreiosShippingResponse>>> CalculateShippingAsync(
            CorreiosShippingRequest request,
            CorreiosCredentials? credentials = null,
            CancellationToken cancellationToken = default);
    }
}
