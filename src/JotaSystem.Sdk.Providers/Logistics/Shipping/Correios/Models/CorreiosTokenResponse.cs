using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.Correios.Models
{
    internal class CorreiosTokenResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("expiraEm")]
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
