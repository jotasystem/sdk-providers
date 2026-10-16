using System.Text.Json;
using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution
{
    /// <summary>
    /// Serializacao dedicada a Evolution API: camelCase e sem nos nulos, para que campos
    /// opcionais nao enviados mantenham o padrao do servidor.
    /// </summary>
    internal static class EvolutionApiJson
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        internal static string Serialize<T>(T value) => JsonSerializer.Serialize(value, _options);

        /// <summary>Le uma propriedade sem diferenciar maiusculas, seguindo o caminho informado.</summary>
        internal static bool TryGet(JsonElement element, out JsonElement value, params string[] path)
        {
            value = element;
            foreach (var name in path)
            {
                if (value.ValueKind != JsonValueKind.Object)
                    return false;

                var found = false;
                foreach (var property in value.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);
        }

        internal static string? ReadString(JsonElement element, params string[] path)
        {
            if (!TryGet(element, out var value, path))
                return null;

            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
                _ => null
            };

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        internal static int? ReadInt(JsonElement element, params string[] path)
        {
            if (!TryGet(element, out var value, path))
                return null;

            return value.ValueKind switch
            {
                JsonValueKind.Number when value.TryGetInt32(out var number) => number,
                JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        internal static long? ReadLong(JsonElement element, params string[] path)
        {
            if (!TryGet(element, out var value, path))
                return null;

            return value.ValueKind switch
            {
                JsonValueKind.Number when value.TryGetInt64(out var number) => number,
                JsonValueKind.String when long.TryParse(value.GetString(), out var parsed) => parsed,
                // Algumas versoes serializam o timestamp do Baileys como { low, high, unsigned }.
                JsonValueKind.Object => ReadLong(value, "low"),
                _ => null
            };
        }

        internal static bool? ReadBool(JsonElement element, params string[] path)
        {
            if (!TryGet(element, out var value, path))
                return null;

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
                _ => null
            };
        }
    }
}
