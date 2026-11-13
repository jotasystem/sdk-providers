using JotaSystem.Sdk.Core.CrossCutting.Providers.Models;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Controle da recorrencia agendada na Cielo usando as credenciais da integracao do tenant,
    /// resolvidas do mesmo jeito que nas cobrancas feitas pelo contrato de gateway.
    /// </summary>
    public interface ICieloRecurrenceGateway
    {
        /// <summary>
        /// Desativa a recorrencia: a Cielo para de gerar as cobrancas dos proximos ciclos.
        /// Desativar uma recorrencia ja desativada nao e erro.
        /// </summary>
        Task<PaymentProviderResult> DeactivateRecurrenceAsync(
            string recurrentPaymentId,
            PaymentProviderContext? context,
            CancellationToken cancellationToken = default);
    }
}
