using JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link
{
    /// <summary>
    /// Integracao com a API Link de Pagamento da Cielo (Checkout Cielo). Cria a pagina de
    /// pagamento hospedada pela Cielo, onde o pagador escolhe o meio de pagamento e informa
    /// os dados do cartao, e consulta o ciclo de vida dos pedidos gerados por ela.
    /// </summary>
    /// <remarks>
    /// E uma API distinta da API E-commerce: autentica por OAuth2 com as credenciais do
    /// Link (<see cref="CieloLinkCredentials"/>) e nao usa <c>MerchantId</c> e
    /// <c>MerchantKey</c>. Quando <c>credentials</c> nao e informado, valem as credenciais
    /// padrao de <see cref="CieloOptions.DefaultLinkCredentials"/>.
    /// </remarks>
    public interface ICieloLinkProvider
    {
        /// <summary>Cria um link de pagamento (<c>POST /api/public/v1/products/</c>).</summary>
        Task<ApiResponse<CieloLinkResponse>> CreateLinkAsync(
            CieloLinkRequest request,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Consulta um link pelo identificador devolvido na criacao.</summary>
        Task<ApiResponse<CieloLinkResponse>> GetLinkAsync(
            string linkId,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Exclui um link, impedindo novos pagamentos por ele.</summary>
        Task<ApiResponse<bool>> DeleteLinkAsync(
            string linkId,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Lista os pedidos gerados por um link, do mais recente para o mais antigo.</summary>
        Task<ApiResponse<CieloLinkOrderList>> GetLinkOrdersAsync(
            string linkId,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Consulta um pedido pelo <c>checkout_cielo_order_number</c> da notificacao.</summary>
        Task<ApiResponse<CieloLinkOrder>> GetOrderAsync(
            string checkoutOrderNumber,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela ou estorna um pedido. Sem <paramref name="amount"/> o cancelamento e total.
        /// </summary>
        Task<ApiResponse<CieloLinkOperationResponse>> VoidOrderAsync(
            string checkoutOrderNumber,
            long? amount = null,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default);
    }
}
