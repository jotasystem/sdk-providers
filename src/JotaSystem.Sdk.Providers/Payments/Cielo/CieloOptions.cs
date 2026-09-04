using JotaSystem.Sdk.Providers.Payments.Cielo.Models;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Configuracao da integracao com a API E-commerce da Cielo.
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
    }
}
