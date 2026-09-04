using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Resposta da consulta de vendas por <c>MerchantOrderId</c>.
    /// </summary>
    public class CieloMerchantOrderResponse
    {
        public List<CieloMerchantOrderPayment> Payments { get; set; } = [];
    }

    /// <summary>
    /// Pagamento resumido devolvido na consulta por <c>MerchantOrderId</c>.
    /// </summary>
    public class CieloMerchantOrderPayment
    {
        public string? PaymentId { get; set; }

        /// <summary>Data de recebimento. A Cielo devolve esse campo com o nome <c>ReceveidDate</c>.</summary>
        [JsonPropertyName("ReceveidDate")]
        public string? ReceivedDate { get; set; }
    }
}
