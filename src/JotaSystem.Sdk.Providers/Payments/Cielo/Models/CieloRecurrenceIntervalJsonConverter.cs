using System.Text.Json;
using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// A Cielo devolve a periodicidade como numero na criacao da venda e como nome
    /// (<c>"Monthly"</c>, <c>"Bimonthly"</c>...) na consulta da recorrencia. Aceita os dois
    /// formatos e expoe sempre o numero de meses.
    /// </summary>
    internal sealed class CieloRecurrenceIntervalJsonConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.Number:
                    return reader.TryGetInt32(out var number) ? number : null;
                case JsonTokenType.String:
                    var text = reader.GetString();
                    if (string.IsNullOrWhiteSpace(text))
                        return null;
                    if (int.TryParse(text, out var parsedNumber))
                        return parsedNumber;
                    return Enum.TryParse<CieloRecurrenceIntervalEnum>(text.Trim(), ignoreCase: true, out var interval)
                        ? (int)interval
                        : null;
                default:
                    reader.Skip();
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}
