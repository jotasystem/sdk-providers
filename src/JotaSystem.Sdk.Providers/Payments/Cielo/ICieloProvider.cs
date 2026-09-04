using JotaSystem.Sdk.Providers.Payments.Cielo.Models;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Integracao com a API E-commerce da Cielo (versao 3.0). Expoe as operacoes de
    /// cartao de credito, cartao de credito recorrente, Pix e boleto, alem das
    /// consultas e do ciclo de vida da transacao.
    /// </summary>
    /// <remarks>
    /// Quando <c>credentials</c> nao e informado, sao usadas as credenciais padrao de
    /// <see cref="CieloOptions.DefaultCredentials"/>. Informe credenciais por chamada
    /// em cenarios multi-loja.
    /// </remarks>
    public interface ICieloProvider
    {
        /// <summary>
        /// Cria uma venda (<c>POST /1/sales/</c>). O meio de pagamento e definido por
        /// <see cref="CieloPaymentRequest.Type"/>.
        /// </summary>
        Task<ApiResponse<CieloSaleResponse>> CreateSaleAsync(
            CieloSaleRequest request,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Consulta uma venda pelo <c>PaymentId</c>.</summary>
        Task<ApiResponse<CieloSaleResponse>> GetSaleAsync(
            string paymentId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Consulta os pagamentos vinculados a um <c>MerchantOrderId</c>.</summary>
        Task<ApiResponse<List<CieloMerchantOrderPayment>>> GetSalesByMerchantOrderIdAsync(
            string merchantOrderId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Captura uma autorizacao de credito. Sem <paramref name="amount"/> a captura e total.
        /// </summary>
        Task<ApiResponse<CieloOperationResponse>> CaptureAsync(
            string paymentId,
            long? amount = null,
            long? serviceTaxAmount = null,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela ou estorna uma transacao. Sem <paramref name="amount"/> o cancelamento e total.
        /// Tambem e o caminho para solicitar a devolucao de um Pix.
        /// </summary>
        Task<ApiResponse<CieloOperationResponse>> VoidAsync(
            string paymentId,
            long? amount = null,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Consulta uma recorrencia agendada pelo <c>RecurrentPaymentId</c>.</summary>
        Task<ApiResponse<CieloRecurrentPaymentResponse>> GetRecurrentPaymentAsync(
            string recurrentPaymentId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Aplica alteracoes em uma recorrencia agendada.</summary>
        Task<ApiResponse<bool>> UpdateRecurrentPaymentAsync(
            string recurrentPaymentId,
            CieloRecurrentPaymentUpdate update,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Desativa uma recorrencia agendada.</summary>
        Task<ApiResponse<bool>> DeactivateRecurrentPaymentAsync(
            string recurrentPaymentId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Reativa uma recorrencia agendada.</summary>
        Task<ApiResponse<bool>> ReactivateRecurrentPaymentAsync(
            string recurrentPaymentId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default);
    }
}
