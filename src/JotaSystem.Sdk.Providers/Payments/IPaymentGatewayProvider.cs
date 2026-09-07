using JotaSystem.Sdk.Core.CrossCutting.Providers.Models;

namespace JotaSystem.Sdk.Providers.Payments
{
    public interface IPaymentGatewayProvider
    {
        string ProviderKey { get; }

        /// <summary>
        /// Abre uma sessao de checkout para captura do cartao no navegador do comprador.
        /// Gateways sem esse recurso mantem a implementacao padrao.
        /// </summary>
        Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
            PaymentCheckoutSessionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PaymentCheckoutSession(
                false,
                Message: $"O gateway '{ProviderKey}' nao oferece captura de cartao no navegador."));

        Task<PaymentProviderResult> CreateAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default);
        Task<PaymentProviderResult> GetAsync(PaymentProviderQuery query, CancellationToken cancellationToken = default);
        Task<PaymentProviderResult> CancelAsync(PaymentProviderOperation operation, CancellationToken cancellationToken = default);
        Task<PaymentProviderResult> RefundAsync(PaymentProviderRefund operation, CancellationToken cancellationToken = default);
        Task<PaymentWebhookEvent> ParseWebhookAsync(PaymentWebhookRequest request, CancellationToken cancellationToken = default);
    }
}
