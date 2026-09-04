using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Resposta da criacao e da consulta de venda na Cielo.
    /// </summary>
    public class CieloSaleResponse
    {
        public string? MerchantOrderId { get; set; }
        public CieloCustomer? Customer { get; set; }
        public CieloPaymentResponse Payment { get; set; } = new();

        /// <summary>Corpo original devolvido pela Cielo, preservado para auditoria.</summary>
        [JsonIgnore]
        public string? RawPayload { get; set; }
    }

    /// <summary>
    /// No <c>Payment</c> da resposta da Cielo, comum a credito, recorrencia, Pix e boleto.
    /// </summary>
    public class CieloPaymentResponse
    {
        public string? PaymentId { get; set; }
        public string? Type { get; set; }
        public long Amount { get; set; }
        public long? CapturedAmount { get; set; }
        public string? Currency { get; set; }
        public string? Country { get; set; }
        public string? Provider { get; set; }
        public int? Installments { get; set; }
        public bool? Capture { get; set; }
        public bool? Authenticate { get; set; }
        public bool? Recurrent { get; set; }
        public string? SoftDescriptor { get; set; }

        /// <summary>Status da transacao. Ver <see cref="CieloPaymentStatusEnum"/>.</summary>
        public int Status { get; set; }

        public string? ReturnCode { get; set; }
        public string? ReturnMessage { get; set; }
        public int? ReasonCode { get; set; }
        public string? ReasonMessage { get; set; }

        public string? Tid { get; set; }
        public string? ProofOfSale { get; set; }
        public string? AuthorizationCode { get; set; }
        public string? AcquirerTransactionId { get; set; }
        public string? IssuerTransactionId { get; set; }
        public string? SentOrderId { get; set; }

        /// <summary>Data de recebimento no formato <c>yyyy-MM-dd HH:mm:ss</c>.</summary>
        public string? ReceivedDate { get; set; }

        /// <summary>Data de captura no formato <c>yyyy-MM-dd HH:mm:ss</c>.</summary>
        public string? CapturedDate { get; set; }

        public CieloCreditCardResponse? CreditCard { get; set; }
        public CieloRecurrentPaymentResponse? RecurrentPayment { get; set; }

        /// <summary>Url do boleto ou da pagina de pagamento.</summary>
        public string? Url { get; set; }

        public string? BoletoNumber { get; set; }
        public string? BarCodeNumber { get; set; }
        public string? DigitableLine { get; set; }
        public string? Assignor { get; set; }
        public string? Instructions { get; set; }
        public string? Demonstrative { get; set; }
        public string? Identification { get; set; }

        /// <summary>Vencimento do boleto no formato <c>yyyy-MM-dd</c>.</summary>
        public string? ExpirationDate { get; set; }

        /// <summary>Codigo copia e cola do Pix.</summary>
        public string? QrCodeString { get; set; }

        /// <summary>Imagem do QRCode Pix em base64.</summary>
        [JsonPropertyName("QrcodeBase64Image")]
        public string? QrCodeBase64Image { get; set; }

        public List<CieloLink> Links { get; set; } = [];
    }

    /// <summary>
    /// Dados do cartao retornados pela Cielo, com o numero mascarado.
    /// </summary>
    public class CieloCreditCardResponse
    {
        public string? CardNumber { get; set; }
        public string? Holder { get; set; }
        public string? ExpirationDate { get; set; }
        public string? Brand { get; set; }
        public bool? SaveCard { get; set; }
        public string? CardToken { get; set; }
        public string? PaymentAccountReference { get; set; }
    }

    /// <summary>
    /// No <c>RecurrentPayment</c> retornado na criacao e na consulta da recorrencia.
    /// </summary>
    public class CieloRecurrentPaymentResponse
    {
        public string? RecurrentPaymentId { get; set; }
        public int? ReasonCode { get; set; }
        public string? ReasonMessage { get; set; }
        public bool? AuthorizeNow { get; set; }

        /// <summary>Data da proxima cobranca no formato <c>yyyy-MM-dd</c>.</summary>
        public string? NextRecurrency { get; set; }

        public string? StartDate { get; set; }
        public string? EndDate { get; set; }

        /// <summary>Periodicidade em meses. Ver <see cref="CieloRecurrenceIntervalEnum"/>.</summary>
        public int? Interval { get; set; }

        public int? RecurrencyDay { get; set; }
        public long? Amount { get; set; }
        public string? Currency { get; set; }
        public string? Country { get; set; }
        public bool? Successful { get; set; }
        public List<CieloLink> Links { get; set; } = [];

        /// <summary>Corpo original devolvido pela Cielo, preservado para auditoria.</summary>
        [JsonIgnore]
        public string? RawPayload { get; set; }
    }

    /// <summary>
    /// Link HATEOAS devolvido pela Cielo.
    /// </summary>
    public class CieloLink
    {
        public string? Method { get; set; }
        public string? Rel { get; set; }
        public string? Href { get; set; }
    }
}
