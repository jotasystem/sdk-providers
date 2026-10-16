namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution
{
    /// <summary>
    /// Normaliza numeros para o formato aceito pela Evolution API: apenas digitos, com DDI.
    /// </summary>
    public static class EvolutionPhoneNumber
    {
        private const string JidSeparator = "@";

        /// <summary>
        /// Devolve o numero apenas com digitos e com DDI. Numeros nacionais (10 ou 11 digitos)
        /// recebem <paramref name="defaultCountryCode"/>. JIDs (<c>...@s.whatsapp.net</c>,
        /// <c>...@g.us</c>) sao mantidos como vieram. Devolve nulo quando nao ha um numero valido.
        /// </summary>
        public static string? Normalize(string? value, string defaultCountryCode = "55")
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (trimmed.Contains(JidSeparator, StringComparison.Ordinal))
                return trimmed;

            var digits = new string(trimmed.Where(char.IsDigit).ToArray()).TrimStart('0');
            if (digits.Length is 10 or 11)
                digits = $"{defaultCountryCode}{digits}";

            return digits.Length is >= 12 and <= 15 ? digits : null;
        }

        /// <summary>Extrai os digitos do numero de um JID (<c>5511999999999@s.whatsapp.net</c> ou <c>5511999999999:12@s.whatsapp.net</c>).</summary>
        public static string? FromJid(string? jid)
        {
            if (string.IsNullOrWhiteSpace(jid))
                return null;

            var user = jid.Split('@')[0].Split(':')[0];
            var digits = new string(user.Where(char.IsDigit).ToArray());
            return digits.Length == 0 ? null : digits;
        }

        public static bool IsGroupJid(string? jid) =>
            jid?.EndsWith("@g.us", StringComparison.OrdinalIgnoreCase) == true;
    }
}
