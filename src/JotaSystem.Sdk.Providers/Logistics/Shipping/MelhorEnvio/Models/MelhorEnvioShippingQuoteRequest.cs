using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.MelhorEnvio.Models
{
    public class MelhorEnvioShippingQuoteRequest
    {
        [JsonPropertyName("from")]
        public MelhorEnvioShippingLocationRequest From { get; set; } = new();

        [JsonPropertyName("to")]
        public MelhorEnvioShippingLocationRequest To { get; set; } = new();

        [JsonPropertyName("products")]
        public List<MelhorEnvioShippingProductRequest> Products { get; set; } = [];

        [JsonPropertyName("volumes")]
        public List<MelhorEnvioShippingVolumeRequest> Volumes { get; set; } = [];

        [JsonPropertyName("options")]
        public MelhorEnvioShippingOptionsRequest? Options { get; set; }

        [JsonPropertyName("services")]
        public string? Services { get; set; }
    }

    public class MelhorEnvioShippingLocationRequest
    {
        [JsonPropertyName("postal_code")]
        public string PostalCode { get; set; } = string.Empty;
    }

    public class MelhorEnvioShippingProductRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        [JsonPropertyName("insurance_value")]
        public decimal InsuranceValue { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class MelhorEnvioShippingVolumeRequest
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        [JsonPropertyName("insurance")]
        public decimal Insurance { get; set; }
    }

    public class MelhorEnvioShippingOptionsRequest
    {
        [JsonPropertyName("receipt")]
        public bool Receipt { get; set; }

        [JsonPropertyName("own_hand")]
        public bool OwnHand { get; set; }
    }
}
