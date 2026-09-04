using System.Text.Json;
using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Serializacao dedicada a Cielo. O contrato da API usa PascalCase e rejeita
    /// nos nulos, por isso nao e possivel reaproveitar o serializador camelCase do SDK.
    /// </summary>
    internal static class CieloJson
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        internal static string Serialize<T>(T value) => JsonSerializer.Serialize(value, _options);

        internal static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);
    }
}
