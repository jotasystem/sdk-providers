using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.Correios.Models
{
    public class CorreiosShippingResponse
    {
        [JsonPropertyName("coProduto")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("nuRequisicao")]
        public string RequestNumber { get; set; } = string.Empty;

        [JsonPropertyName("cepOrigem")]
        public string OriginZipCode { get; set; } = string.Empty;

        [JsonPropertyName("cepDestino")]
        public string DestinationZipCode { get; set; } = string.Empty;

        [JsonPropertyName("psCobrado")]
        public string ChargedWeight { get; set; } = string.Empty;

        [JsonPropertyName("pcFinal")]
        public string FinalPrice { get; set; } = string.Empty;

        [JsonPropertyName("vlTotal")]
        public string TotalPrice { get; set; } = string.Empty;

        [JsonPropertyName("txErro")]
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
