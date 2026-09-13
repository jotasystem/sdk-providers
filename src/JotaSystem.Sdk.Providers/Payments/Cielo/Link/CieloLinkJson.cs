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
        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        private static readonly JsonSerializerOptions _readOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,

            // O Checkout Cielo serializa com Newtonsoft — daí o "$id" nas respostas — e alterna
            // entre numero e texto no mesmo campo, entao a leitura aceita as duas formas.
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        internal static string Serialize<T>(T value) => JsonSerializer.Serialize(value, _writeOptions);

        internal static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _readOptions);
    }
}
