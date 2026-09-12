using System.Globalization;
using System.Text.Json;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models
{
    /// <summary>
    /// Notificacao enviada pelo Link de Pagamento a cada tentativa de pagamento e a cada
    /// mudanca de status. A Cielo posta o mesmo conteudo em formulario ou em JSON, por isso
    /// os dois formatos sao aceitos aqui.
    /// </summary>
    public sealed class CieloLinkNotification
    {
        private const string CheckoutOrderNumberKey = "checkoutcieloordernumber";
        private const string OrderNumberKey = "ordernumber";
        private const string ProductIdKey = "productid";
        private const string AmountKey = "amount";
        private const string StatusKey = "paymentstatus";
        private const string MethodTypeKey = "paymentmethodtype";
        private const string TidKey = "tid";
        private const string CreatedDateKey = "createddate";
        private const string BoletoNumberKey = "paymentboletonumber";
        private const string EndToEndIdKey = "paymentendtoendid";
        private const string RecurrentPaymentIdKey = "pagadorrecurrentpaymentid";
        private const string TestTransactionKey = "testtransaction";
        private const string CreatedDateFormat = "dd-MM-yyyy HH:mm:ss";

        private readonly IReadOnlyDictionary<string, string> _values;

        private CieloLinkNotification(IReadOnlyDictionary<string, string> values) => _values = values;

        /// <summary>Identificador do pedido gerado pelo Checkout Cielo.</summary>
        public string? CheckoutCieloOrderNumber => Read(CheckoutOrderNumberKey);

        /// <summary>Numero do pedido informado pela loja na criacao do link.</summary>
        public string? OrderNumber => Read(OrderNumberKey);

        /// <summary>Identificador do link que originou o pagamento.</summary>
        public string? ProductId => Read(ProductIdKey);

        /// <summary>Valor pago em centavos.</summary>
        public long? Amount =>
            long.TryParse(Read(AmountKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount)
                ? amount
                : null;

        public CieloLinkStatusEnum? PaymentStatus => CieloLinkStatusConverter.Parse(Read(StatusKey));

        /// <summary>Meio de pagamento: 1 credito, 2 boleto, 4 debito, 5 QR Code e 6 Pix.</summary>
        public string? PaymentMethodType => Read(MethodTypeKey);

        public string? Tid => Read(TidKey);
        public string? BoletoNumber => Read(BoletoNumberKey);
        public string? EndToEndId => Read(EndToEndIdKey);
        public string? RecurrentPaymentId => Read(RecurrentPaymentIdKey);

        /// <summary>Indica que a transacao nasceu com o modo de teste da loja ligado.</summary>
        public bool IsTest => bool.TryParse(Read(TestTransactionKey), out var isTest) && isTest;

        /// <summary>Criacao do pedido, no formato <c>dd-MM-yyyy HH:mm:ss</c>.</summary>
        public DateTimeOffset? CreatedDate =>
            DateTime.TryParseExact(
                Read(CreatedDateKey),
                CreatedDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date)
                ? new DateTimeOffset(date, TimeSpan.Zero)
                : null;

        /// <summary>
        /// Le a notificacao em JSON ou em formulario. Devolve <c>null</c> quando o conteudo
        /// nao tem os campos que identificam um pedido do Link de Pagamento.
        /// </summary>
        public static CieloLinkNotification? TryParse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return null;

            var values = payload.TrimStart().StartsWith('{')
                ? ReadJson(payload)
                : ReadForm(payload);

            if (values is null)
                return null;

            var notification = new CieloLinkNotification(values);

            return notification.CheckoutCieloOrderNumber is null && notification.ProductId is null
                ? null
                : notification;
        }

        private string? Read(string key) =>
            _values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;

        private static Dictionary<string, string>? ReadJson(string payload)
        {
            try
            {
                using var document = JsonDocument.Parse(payload);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    var value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False =>
                            property.Value.GetRawText(),
                        _ => null
                    };

                    if (value is not null)
                        values[Normalize(property.Name)] = value;
                }

                return values;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static Dictionary<string, string> ReadForm(string payload)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var pair in payload.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = pair.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = Decode(pair[..separator]);
                if (key.Length > 0)
                    values[Normalize(key)] = Decode(pair[(separator + 1)..]);
            }

            return values;
        }

        // Formulario postado como application/x-www-form-urlencoded: o espaco chega como '+'.
        private static string Decode(string value) =>
            Uri.UnescapeDataString(value.Replace('+', ' '));

        // A Cielo alterna entre snake_case no formulario e camelCase no JSON, entao a chave
        // e reduzida a letras e digitos para que os dois formatos caiam no mesmo lugar.
        private static string Normalize(string key) =>
            new([.. key.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
    }
}
