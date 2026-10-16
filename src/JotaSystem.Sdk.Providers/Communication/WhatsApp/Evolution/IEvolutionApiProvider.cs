using JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution.Models;

namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution
{
    /// <summary>
    /// Integracao com a Evolution API (v2): ciclo de vida das instancias de WhatsApp,
    /// pareamento por QR Code, configuracao do webhook e envio de mensagens.
    /// </summary>
    /// <remarks>
    /// Quando <c>credentials</c> nao e informado, e usado o servidor de
    /// <see cref="EvolutionApiOptions.DefaultCredentials"/>. Informe credenciais por chamada
    /// quando cada cliente tem um servidor proprio.
    /// </remarks>
    public interface IEvolutionApiProvider
    {
        /// <summary>Consulta a raiz do servidor (<c>GET /</c>) para confirmar que ele responde e qual a versao.</summary>
        Task<ApiResponse<EvolutionServerInfo>> GetServerInfoAsync(
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista as instancias do servidor. Com <paramref name="instanceName"/>, apenas aquela.
        /// Tambem serve para validar a chave: o servidor recusa chave invalida com 401.
        /// </summary>
        Task<ApiResponse<List<EvolutionInstanceInfo>>> FetchInstancesAsync(
            string? instanceName = null,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Cria a instancia e, com <see cref="EvolutionCreateInstanceRequest.QrCode"/>, devolve o primeiro QR Code.</summary>
        Task<ApiResponse<EvolutionCreateInstanceResponse>> CreateInstanceAsync(
            EvolutionCreateInstanceRequest request,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inicia (ou renova) o pareamento e devolve o QR Code. Com <paramref name="phoneNumber"/>,
        /// devolve tambem o codigo de pareamento para digitar no celular.
        /// </summary>
        Task<ApiResponse<EvolutionConnectResponse>> ConnectInstanceAsync(
            string instanceName,
            string? phoneNumber = null,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Estado atual da sessao do WhatsApp.</summary>
        Task<ApiResponse<EvolutionConnectionState>> GetConnectionStateAsync(
            string instanceName,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Reinicia a sessao sem desparear o numero.</summary>
        Task<ApiResponse<bool>> RestartInstanceAsync(
            string instanceName,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Desconecta o numero (o aparelho deixa de aparecer em "Aparelhos conectados").</summary>
        Task<ApiResponse<bool>> LogoutInstanceAsync(
            string instanceName,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Remove a instancia do servidor.</summary>
        Task<ApiResponse<bool>> DeleteInstanceAsync(
            string instanceName,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Configura o webhook da instancia.</summary>
        Task<ApiResponse<bool>> SetWebhookAsync(
            string instanceName,
            EvolutionWebhookConfig webhook,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Altera o comportamento da sessao (recusar chamadas, ignorar grupos...).</summary>
        Task<ApiResponse<bool>> SetSettingsAsync(
            string instanceName,
            EvolutionInstanceSettings settings,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Envia uma mensagem de texto.</summary>
        Task<ApiResponse<EvolutionMessageResponse>> SendTextAsync(
            string instanceName,
            EvolutionSendTextRequest request,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Envia imagem, video, audio ou documento, por URL ou base64.</summary>
        Task<ApiResponse<EvolutionMessageResponse>> SendMediaAsync(
            string instanceName,
            EvolutionSendMediaRequest request,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Confere quais numeros tem conta no WhatsApp.</summary>
        Task<ApiResponse<List<EvolutionNumberCheck>>> CheckWhatsAppNumbersAsync(
            string instanceName,
            IEnumerable<string> numbers,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default);

        /// <summary>Le a notificacao recebida no webhook. Ver <see cref="EvolutionWebhookParser"/>.</summary>
        ApiResponse<EvolutionWebhookEvent> ParseWebhook(string payload);
    }
}
