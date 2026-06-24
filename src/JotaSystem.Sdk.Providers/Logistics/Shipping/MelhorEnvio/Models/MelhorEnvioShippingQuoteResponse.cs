using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.MelhorEnvio.Models
{
    public class MelhorEnvioShippingQuoteResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public string Price { get; set; } = string.Empty;

        [JsonPropertyName("custom_price")]
        public string CustomPrice { get; set; } = string.Empty;

        [JsonPropertyName("discount")]
        public string Discount { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("delivery_time")]
        public int DeliveryTime { get; set; }

        [JsonPropertyName("delivery_range")]
        public MelhorEnvioDeliveryRangeResponse? DeliveryRange { get; set; }

        [JsonPropertyName("custom_delivery_time")]
        public int CustomDeliveryTime { get; set; }

        [JsonPropertyName("custom_delivery_range")]
        public MelhorEnvioDeliveryRangeResponse? CustomDeliveryRange { get; set; }

        [JsonPropertyName("packages")]
        public List<MelhorEnvioPackageResponse> Packages { get; set; } = [];

        [JsonPropertyName("additional_services")]
        public MelhorEnvioAdditionalServicesResponse? AdditionalServices { get; set; }

        [JsonPropertyName("company")]
        public MelhorEnvioCompanyResponse? Company { get; set; }
    }

    public class MelhorEnvioDeliveryRangeResponse
    {
        [JsonPropertyName("min")]
        public int Min { get; set; }

        [JsonPropertyName("max")]
        public int Max { get; set; }
    }

    public class MelhorEnvioPackageResponse
    {
        [JsonPropertyName("price")]
        public string Price { get; set; } = string.Empty;

        [JsonPropertyName("discount")]
        public string Discount { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;

        [JsonPropertyName("dimensions")]
        public MelhorEnvioDimensionsResponse? Dimensions { get; set; }

        [JsonPropertyName("weight")]
        public string Weight { get; set; } = string.Empty;

        [JsonPropertyName("insurance_value")]
        public string InsuranceValue { get; set; } = string.Empty;

        [JsonPropertyName("products")]
        public List<MelhorEnvioPackageProductResponse> Products { get; set; } = [];
    }

    public class MelhorEnvioDimensionsResponse
    {
        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("length")]
        public int Length { get; set; }
    }

    public class MelhorEnvioPackageProductResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class MelhorEnvioAdditionalServicesResponse
    {
        [JsonPropertyName("receipt")]
        public bool Receipt { get; set; }

        [JsonPropertyName("own_hand")]
        public bool OwnHand { get; set; }

        [JsonPropertyName("collect")]
        public bool Collect { get; set; }
    }

    public class MelhorEnvioCompanyResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("picture")]
        public string Picture { get; set; } = string.Empty;
    }
}
