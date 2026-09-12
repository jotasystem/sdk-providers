using System.Text.Json;
using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link
{
    /// <summary>
    /// Serializacao da API Link de Pagamento. Diferente da API E-commerce, o contrato do
    /// Checkout Cielo usa camelCase, por isso ele nao compartilha o serializador da
    /// <see cref="CieloJson"/>.
    /// </summary>
    internal static class CieloLinkJson
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        internal static string Serialize<T>(T value) => JsonSerializer.Serialize(value, _options);

        internal static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);
    }
}
