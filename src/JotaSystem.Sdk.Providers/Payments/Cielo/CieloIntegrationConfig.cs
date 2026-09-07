using System.Text.Json;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Configuracao publica da integracao, gravada pelo sistema consumidor em formato JSON.
    /// </summary>
    internal sealed class CieloIntegrationConfig
    {
        internal static readonly CieloIntegrationConfig Empty = new();

        internal string? MerchantId { get; private set; }
        internal string? MerchantKey { get; private set; }
        internal string? ClientId { get; private set; }
        internal string? SoftDescriptor { get; private set; }
        internal string? BoletoProvider { get; private set; }
        internal string? BoletoAssignor { get; private set; }
        internal string? BoletoInstructions { get; private set; }
        internal string? BoletoDemonstrative { get; private set; }
        internal string? RecurrenceInterval { get; private set; }
        internal string? WebhookHeaderName { get; private set; }
        internal bool? Capture { get; private set; }
        internal int? BoletoExpirationDays { get; private set; }

        internal static CieloIntegrationConfig Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Empty;

            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return Empty;

                var root = document.RootElement;
                return new CieloIntegrationConfig
                {
                    MerchantId = ReadString(root, "merchantId"),
                    MerchantKey = ReadString(root, "merchantKey"),
                    ClientId = ReadString(root, "clientId"),
                    SoftDescriptor = ReadString(root, "softDescriptor"),
                    BoletoProvider = ReadString(root, "boletoProvider"),
                    BoletoAssignor = ReadString(root, "boletoAssignor"),
                    BoletoInstructions = ReadString(root, "boletoInstructions"),
                    BoletoDemonstrative = ReadString(root, "boletoDemonstrative"),
                    RecurrenceInterval = ReadString(root, "recurrenceInterval"),
                    WebhookHeaderName = ReadString(root, "webhookHeaderName"),
                    Capture = ReadBoolean(root, "capture"),
                    BoletoExpirationDays = ReadInteger(root, "boletoExpirationDays")
                };
            }
            catch (JsonException)
            {
                return Empty;
            }
        }

        private static string? ReadString(JsonElement root, string propertyName)
        {
            if (!TryGet(root, propertyName, out var property))
                return null;

            var value = property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                _ => null
            };

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool? ReadBoolean(JsonElement root, string propertyName)
        {
            if (!TryGet(root, propertyName, out var property))
                return null;

            return property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(property.GetString(), out var parsed) ? parsed : null,
                _ => null
            };
        }

        private static int? ReadInteger(JsonElement root, string propertyName)
        {
            if (!TryGet(root, propertyName, out var property))
                return null;

            return property.ValueKind switch
            {
                JsonValueKind.Number when property.TryGetInt32(out var number) => number,
                JsonValueKind.String when int.TryParse(property.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        private static bool TryGet(JsonElement root, string propertyName, out JsonElement property)
        {
            foreach (var candidate in root.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }

            property = default;
            return false;
        }
    }
}
