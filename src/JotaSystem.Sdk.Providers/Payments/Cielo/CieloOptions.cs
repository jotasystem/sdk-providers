using JotaSystem.Sdk.Providers.Payments.Cielo.Link;
using JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models;
using JotaSystem.Sdk.Providers.Payments.Cielo.Models;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Configuracao da integracao com as APIs da Cielo: a API E-commerce e a API Link de
    /// Pagamento, que tem credenciais e endereco proprios.
    /// </summary>
    public class CieloOptions
    {
        /// <summary>Credenciais usadas quando a chamada nao informa credenciais proprias.</summary>
        public CieloCredentials? DefaultCredentials { get; set; }

        /// <summary>
        /// Credenciais nomeadas para cenarios multi-loja. A chave e o mesmo valor gravado
        /// na integracao do sistema consumidor (ex.: <c>SecretReference</c>).
        /// </summary>
        public Dictionary<string, CieloCredentials> NamedCredentials { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public string SandboxTransactionUrl { get; set; } = "https://apisandbox.cieloecommerce.cielo.com.br/";
        public string SandboxQueryUrl { get; set; } = "https://apiquerysandbox.cieloecommerce.cielo.com.br/";
        public string ProductionTransactionUrl { get; set; } = "https://api.cieloecommerce.cielo.com.br/";
        public string ProductionQueryUrl { get; set; } = "https://apiquery.cieloecommerce.cielo.com.br/";

        /// <summary>Autenticador OAuth2 usado para abrir a sessao do Silent Order Post.</summary>
        public string SandboxAuthUrl { get; set; } = "https://authsandbox.braspag.com.br/oauth2/token";
        public string ProductionAuthUrl { get; set; } = "https://auth.braspag.com.br/oauth2/token";

        /// <summary>Emissor do AccessToken do Silent Order Post.</summary>
        public string SandboxSilentOrderPostUrl { get; set; } = "https://transactionsandbox.pagador.com.br/post/api/public/v2/accesstoken";
        public string ProductionSilentOrderPostUrl { get; set; } = "https://www.pagador.com.br/post/api/public/v2/accesstoken";

        /// <summary>Script que captura o cartao no navegador do comprador.</summary>
        public string SandboxSilentOrderPostScriptUrl { get; set; } = "https://transactionsandbox.pagador.com.br/post/Scripts/silentorderpost-1.0.min.js";
        public string ProductionSilentOrderPostScriptUrl { get; set; } = "https://transactionscus.pagador.com.br/post/Scripts/silentorderpost-1.0.min.js";

        /// <summary>Margem de renovacao do token OAuth2 antes do vencimento.</summary>
        public TimeSpan AuthTokenExpirationMargin { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Texto exibido na fatura do portador (maximo 13 caracteres).</summary>
        public string? SoftDescriptor { get; set; }

        /// <summary>Captura automatica da autorizacao de credito (<c>Payment.Capture</c>).</summary>
        public bool CaptureCreditCardOnAuthorization { get; set; } = true;

        /// <summary>Banco emissor do boleto (<c>Bradesco2</c>, <c>BancoDoBrasil3</c> ou <c>Simulado</c>).</summary>
        public string BoletoProvider { get; set; } = "Bradesco2";

        /// <summary>Cedente impresso no boleto.</summary>
        public string? BoletoAssignor { get; set; }

        /// <summary>Instrucoes impressas no boleto.</summary>
        public string? BoletoInstructions { get; set; }

        /// <summary>Demonstrativo impresso no boleto.</summary>
        public string? BoletoDemonstrative { get; set; }

        /// <summary>Prazo padrao de vencimento do boleto, em dias, quando nao informado na cobranca.</summary>
        public int BoletoExpirationDays { get; set; } = 3;

        /// <summary>Periodicidade padrao da recorrencia agendada.</summary>
        public CieloRecurrenceIntervalEnum RecurrenceInterval { get; set; } = CieloRecurrenceIntervalEnum.Monthly;

        /// <summary>
        /// Nome do header customizado configurado no Backoffice Cielo para validar o post de notificacao.
        /// Quando informado, o webhook so e aceito se o header chegar com o segredo esperado.
        /// </summary>
        public string? WebhookHeaderName { get; set; }

        /// <summary>
        /// Credenciais padrao da API Link de Pagamento, usadas quando a integracao do tenant
        /// nao informa as proprias.
        /// </summary>
        public CieloLinkCredentials? DefaultLinkCredentials { get; set; }

        /// <summary>
        /// Endereco da API Link de Pagamento. O produto nao tem ambiente de sandbox: o teste
        /// e feito ligando o modo de teste da loja no Backoffice Cielo.
        /// </summary>
        public string LinkApiUrl { get; set; } = "https://cieloecommerce.cielo.com.br/";

        /// <summary>Natureza padrao do link. Ver <see cref="CieloLinkProductTypes"/>.</summary>
        public string LinkProductType { get; set; } = CieloLinkProductTypes.Payment;

        /// <summary>Frete padrao do link. Ver <see cref="CieloLinkShippingTypes"/>.</summary>
        public string LinkShippingType { get; set; } = CieloLinkShippingTypes.WithoutShipping;

        /// <summary>
        /// Prazo padrao de expiracao do link, em dias, quando a cobranca nao informa um.
        /// Sem valor, o link nao expira.
        /// </summary>
        public int? LinkExpirationDays { get; set; }

        /// <summary>
        /// Numero maximo de parcelas oferecido na pagina do link. Sem valor, vale o que
        /// estiver configurado na loja.
        /// </summary>
        public int? LinkMaxInstallments { get; set; }

        /// <summary>
        /// Meios de pagamento liberados na pagina do link. Vazio mantem a configuracao da
        /// loja. Ver <see cref="CieloLinkPaymentTypes"/>.
        /// </summary>
        public IList<string> LinkPaymentTypes { get; } = [];
    }
}
