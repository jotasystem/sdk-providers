using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models
{
    /// <summary>
    /// Link de pagamento criado ou consultado. O link ainda nao e a transacao: ela so
    /// nasce quando o pagador conclui o pagamento na pagina.
    /// </summary>
    public class CieloLinkResponse
    {
        /// <summary>Identificador do link na Cielo.</summary>
        public string? Id { get; set; }

        /// <summary>URL curta para compartilhar com o pagador.</summary>
        public string? ShortUrl { get; set; }

        public string? OrderNumber { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public long? Price { get; set; }
        public string? Sku { get; set; }

        /// <summary>Criacao do link no formato <c>yyyy-MM-dd HH:mm:ss</c>.</summary>
        public string? CreatedDate { get; set; }

        /// <summary>Vencimento do link, quando informado na criacao.</summary>
        public string? ExpirationDate { get; set; }

        public List<CieloLinkHref> Links { get; set; } = [];

        /// <summary>Corpo original devolvido pela Cielo, preservado para auditoria.</summary>
        [JsonIgnore]
        public string? RawPayload { get; set; }
    }

    /// <summary>Link HATEOAS devolvido pela API do Link de Pagamento.</summary>
    public class CieloLinkHref
    {
        public string? Method { get; set; }
        public string? Rel { get; set; }
        public string? Href { get; set; }
    }

    /// <summary>Pagamentos gerados por um link (<c>GET /products/{id}/payments</c>).</summary>
    public class CieloLinkOrderList
    {
        public string? ProductId { get; set; }
        public string? CreatedDate { get; set; }
        public List<CieloLinkOrder> Orders { get; set; } = [];

        /// <summary>Corpo original devolvido pela Cielo, preservado para auditoria.</summary>
        [JsonIgnore]
        public string? RawPayload { get; set; }
    }

    /// <summary>
    /// Pedido gerado a partir do link, tanto na listagem por link quanto na consulta por
    /// <c>checkout_cielo_order_number</c>.
    /// </summary>
    public class CieloLinkOrder
    {
        public string? MerchantId { get; set; }

        /// <summary>Numero do pedido informado pela loja na criacao do link.</summary>
        public string? OrderNumber { get; set; }

        /// <summary>Identificador do pedido gerado pelo Checkout Cielo.</summary>
        [JsonPropertyName("checkoutCieloOrderNumber")]
        public string? CheckoutCieloOrderNumber { get; set; }

        public string? CreatedDate { get; set; }
        public CieloLinkPayment? Payment { get; set; }
        public CieloLinkCustomer? Customer { get; set; }
        public List<CieloLinkHref> Links { get; set; } = [];

        /// <summary>Corpo original devolvido pela Cielo, preservado para auditoria.</summary>
        [JsonIgnore]
        public string? RawPayload { get; set; }
    }

    /// <summary>Pagamento de um pedido do Link de Pagamento.</summary>
    public class CieloLinkPayment
    {
        /// <summary>Valor em centavos.</summary>
        public long? Price { get; set; }

        public int? NumberOfPayments { get; set; }
        public string? CreatedDate { get; set; }

        [JsonConverter(typeof(CieloLinkStatusConverter))]
        public CieloLinkStatusEnum? Status { get; set; }

        public string? Tid { get; set; }
        public string? Nsu { get; set; }
        public string? AuthorizationCode { get; set; }
        public string? PaymentMethodType { get; set; }
        public string? MaskedCreditCard { get; set; }
        public string? BoletoNumber { get; set; }
        public string? BoletoExpirationDate { get; set; }
        public string? QrCodeUrl { get; set; }
    }

    /// <summary>Pagador informado na pagina do link.</summary>
    public class CieloLinkCustomer
    {
        public string? Identity { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    /// <summary>Resposta das operacoes de captura e cancelamento do Checkout Cielo.</summary>
    public class CieloLinkOperationResponse
    {
        public bool Success { get; set; }

        [JsonConverter(typeof(CieloLinkStatusConverter))]
        public CieloLinkStatusEnum? Status { get; set; }

        public string? ReturnCode { get; set; }
        public string? ReturnMessage { get; set; }
        public List<CieloLinkHref> Links { get; set; } = [];

        /// <summary>Corpo original devolvido pela Cielo, preservado para auditoria.</summary>
        [JsonIgnore]
        public string? RawPayload { get; set; }
    }
}
