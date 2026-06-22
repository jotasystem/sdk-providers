using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.Correios.Models
{
    public class CorreiosShippingRequest
    {
        [JsonPropertyName("idLote")]
        public string BatchId { get; set; } = Guid.NewGuid().ToString("N");

        [JsonPropertyName("parametrosProduto")]
        public List<CorreiosShippingProductRequest> Products { get; set; } = [];
    }

    public class CorreiosShippingProductRequest
    {
        [JsonPropertyName("coProduto")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("nuRequisicao")]
        public string RequestNumber { get; set; } = "1";

        [JsonPropertyName("cepOrigem")]
        public string OriginZipCode { get; set; } = string.Empty;

        [JsonPropertyName("cepDestino")]
        public string DestinationZipCode { get; set; } = string.Empty;

        [JsonPropertyName("psObjeto")]
        public string Weight { get; set; } = string.Empty;

        [JsonPropertyName("tpObjeto")]
        public string ObjectType { get; set; } = "2";

        [JsonPropertyName("comprimento")]
        public string Length { get; set; } = string.Empty;

        [JsonPropertyName("largura")]
        public string Width { get; set; } = string.Empty;

        [JsonPropertyName("altura")]
        public string Height { get; set; } = string.Empty;

        [JsonPropertyName("diametro")]
        public string Diameter { get; set; } = "0";

        [JsonPropertyName("servicosAdicionais")]
        public List<string> AdditionalServices { get; set; } = [];
    }
}
