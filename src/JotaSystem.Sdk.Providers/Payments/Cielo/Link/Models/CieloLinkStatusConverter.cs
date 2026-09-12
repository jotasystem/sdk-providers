using System.Text.Json;
using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models
{
    /// <summary>
    /// Le o status da transacao nos dois formatos que a Cielo usa: o numero das
    /// notificacoes (<c>2</c>) e o texto das consultas (<c>"Paid"</c>).
    /// </summary>
    internal sealed class CieloLinkStatusConverter : JsonConverter<CieloLinkStatusEnum?>
    {
        public override CieloLinkStatusEnum? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.Number when reader.TryGetInt32(out var number) => Parse(number),
                JsonTokenType.String => Parse(reader.GetString()),
                _ => null
            };

        public override void Write(
            Utf8JsonWriter writer,
            CieloLinkStatusEnum? value,
            JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteNumberValue((int)value.Value);
        }

        internal static CieloLinkStatusEnum? Parse(int status) =>
            Enum.IsDefined(typeof(CieloLinkStatusEnum), status) ? (CieloLinkStatusEnum)status : null;

        internal static CieloLinkStatusEnum? Parse(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            var trimmed = status.Trim();

            return int.TryParse(trimmed, out var number)
                ? Parse(number)
                : Enum.TryParse<CieloLinkStatusEnum>(trimmed, ignoreCase: true, out var parsed) ? parsed : null;
        }
    }
}
