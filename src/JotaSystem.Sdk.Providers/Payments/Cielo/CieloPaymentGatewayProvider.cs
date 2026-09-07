using JotaSystem.Sdk.Core.CrossCutting.Providers.Enum;
using JotaSystem.Sdk.Core.CrossCutting.Providers.Models;
using JotaSystem.Sdk.Providers.Payments.Cielo.Models;
using System.Globalization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Adapta a API E-commerce da Cielo ao contrato de gateway de pagamento do SDK.
    /// </summary>
    internal sealed class CieloPaymentGatewayProvider(ICieloProvider cieloProvider, CieloOptions options)
        : IPaymentGatewayProvider
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string ReceivedDateFormat = "yyyy-MM-dd HH:mm:ss";
        private const int SoftDescriptorMaxLength = 13;
        private const int MerchantOrderIdMaxLength = 50;

        private readonly ICieloProvider _cieloProvider = cieloProvider;
        private readonly CieloOptions _options = options;

        public string ProviderKey => "cielo";

        public async Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
            PaymentCheckoutSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            var config = CieloIntegrationConfig.Parse(request.Context?.PublicConfigJson);
            var credentials = ResolveCredentials(request.Context, config);
            if (credentials is null)
                return new PaymentCheckoutSession(false, Message: MissingCredentialsMessage);

            var response = await _cieloProvider.CreateSilentOrderPostTokenAsync(credentials, cancellationToken);
            if (!response.Success)
                return new PaymentCheckoutSession(false, Message: response.ErrorMessage);

            var token = response.Data!;
            return new PaymentCheckoutSession(
                true,
                token.AccessToken,
                token.ScriptUrl,
                token.Environment,
                ParseExpiration(token.Issued, token.ExpiresIn));
        }

        public async Task<PaymentProviderResult> CreateAsync(
            PaymentProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            var config = CieloIntegrationConfig.Parse(request.Context?.PublicConfigJson);
            var credentials = ResolveCredentials(request.Context, config);
            if (credentials is null)
                return Failure(MissingCredentialsMessage);

            if (!CieloMethodCodes.TryResolve(request.MethodCode, out var method))
                return Failure($"O metodo de pagamento '{request.MethodCode}' nao e suportado pela Cielo.");

            var response = await _cieloProvider.CreateSaleAsync(
                BuildSale(request, method, config),
                credentials,
                cancellationToken);

            return response.Success
                ? MapCreatedSale(response.Data!)
                : Failure(response.ErrorMessage!);
        }

        public async Task<PaymentProviderResult> GetAsync(
            PaymentProviderQuery query,
            CancellationToken cancellationToken = default)
        {
            var config = CieloIntegrationConfig.Parse(query.Context?.PublicConfigJson);
            var credentials = ResolveCredentials(query.Context, config);
            if (credentials is null)
                return Failure(MissingCredentialsMessage);

            var response = await _cieloProvider.GetSaleAsync(query.TransactionId, credentials, cancellationToken);

            return response.Success
                ? MapQueriedSale(response.Data!)
                : Failure(response.ErrorMessage!);
        }

        public async Task<PaymentProviderResult> CancelAsync(
            PaymentProviderOperation operation,
            CancellationToken cancellationToken = default)
        {
            var config = CieloIntegrationConfig.Parse(operation.Context?.PublicConfigJson);
            var credentials = ResolveCredentials(operation.Context, config);
            if (credentials is null)
                return Failure(MissingCredentialsMessage);

            var response = await _cieloProvider.VoidAsync(
                operation.TransactionId,
                amount: null,
                credentials,
                cancellationToken);

            return response.Success
                ? MapOperation(response.Data!, operation.TransactionId, PaymentProviderStatusEnum.Cancelled)
                : Failure(response.ErrorMessage!);
        }

        public async Task<PaymentProviderResult> RefundAsync(
            PaymentProviderRefund operation,
            CancellationToken cancellationToken = default)
        {
            var config = CieloIntegrationConfig.Parse(operation.Context?.PublicConfigJson);
            var credentials = ResolveCredentials(operation.Context, config);
            if (credentials is null)
                return Failure(MissingCredentialsMessage);

            var response = await _cieloProvider.VoidAsync(
                operation.TransactionId,
                operation.Amount.HasValue ? ToCents(operation.Amount.Value) : null,
                credentials,
                cancellationToken);

            return response.Success
                ? MapOperation(response.Data!, operation.TransactionId, PaymentProviderStatusEnum.Refunded)
                : Failure(response.ErrorMessage!);
        }

        public async Task<PaymentWebhookEvent> ParseWebhookAsync(
            PaymentWebhookRequest request,
            CancellationToken cancellationToken = default)
        {
            var config = CieloIntegrationConfig.Parse(request.Context?.PublicConfigJson);
            var credentials = ResolveCredentials(request.Context, config)
                ?? throw new InvalidOperationException(MissingCredentialsMessage);

            var notification = CieloJson.Deserialize<CieloNotification>(request.Payload);
            if (notification is null || string.IsNullOrWhiteSpace(notification.PaymentId))
                throw new InvalidOperationException("Notificacao da Cielo sem PaymentId.");

            EnsureWebhookIsTrusted(request, config);

            var response = await _cieloProvider.GetSaleAsync(notification.PaymentId, credentials, cancellationToken);
            if (!response.Success)
                throw new InvalidOperationException(
                    $"Nao foi possivel confirmar a notificacao na Cielo: {response.ErrorMessage}");

            return BuildWebhookEvent(request, notification, response.Data!);
        }

        private PaymentWebhookEvent BuildWebhookEvent(
            PaymentWebhookRequest request,
            CieloNotification notification,
            CieloSaleResponse sale)
        {
            var payment = sale.Payment;
            var status = MapStatus(payment.Status, payment.Type);
            var metadata = BuildResultMetadata(payment);
            metadata["change_type"] = notification.ChangeType.ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(notification.RecurrentPaymentId))
                metadata["recurrent_payment_id"] = notification.RecurrentPaymentId;

            return new PaymentWebhookEvent(
                ProviderKey,
                $"{notification.PaymentId}-{notification.ChangeType}-{payment.Status}",
                DescribeEvent(notification.ChangeType, status),
                notification.PaymentId!,
                status,
                FromCents(payment.Amount),
                payment.Currency,
                ParseReceivedDate(payment.ReceivedDate),
                sale.RawPayload ?? request.Payload,
                metadata);
        }

        private void EnsureWebhookIsTrusted(PaymentWebhookRequest request, CieloIntegrationConfig config)
        {
            if (string.IsNullOrWhiteSpace(request.WebhookSecret))
                return;

            var headerName = config.WebhookHeaderName ?? _options.WebhookHeaderName;
            var isTrusted = string.IsNullOrWhiteSpace(headerName)
                ? request.Headers.Any(x => string.Equals(x.Value, request.WebhookSecret, StringComparison.Ordinal))
                : request.Headers.Any(x =>
                    string.Equals(x.Key, headerName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Value, request.WebhookSecret, StringComparison.Ordinal));

            if (!isTrusted)
                throw new InvalidOperationException("Notificacao da Cielo sem o segredo configurado.");
        }

        private CieloSaleRequest BuildSale(
            PaymentProviderRequest request,
            CieloPaymentMethodEnum method,
            CieloIntegrationConfig config)
        {
            var amount = ToCents(request.Amount);
            var payment = method switch
            {
                CieloPaymentMethodEnum.Pix => BuildPixPayment(request, amount),
                CieloPaymentMethodEnum.Boleto => BuildBoletoPayment(request, config, amount),
                CieloPaymentMethodEnum.RecurrentCreditCard => BuildRecurrentCreditCardPayment(request, config, amount),
                _ => BuildCreditCardPayment(request, config, amount)
            };

            return new CieloSaleRequest
            {
                MerchantOrderId = BuildMerchantOrderId(request),
                Customer = BuildCustomer(request),
                Payment = payment
            };
        }

        private CieloPaymentRequest BuildCreditCardPayment(
            PaymentProviderRequest request,
            CieloIntegrationConfig config,
            long amount) =>
            new()
            {
                Type = CieloPaymentTypes.CreditCard,
                Amount = amount,
                Currency = request.Currency,
                Installments = request.InstallmentCount < 1 ? 1 : request.InstallmentCount,
                Capture = ReadBoolean(request.Metadata, CieloMetadataKeys.Capture)
                    ?? config.Capture
                    ?? _options.CaptureCreditCardOnAuthorization,
                SoftDescriptor = ResolveSoftDescriptor(request.Metadata, config),
                CreditCard = BuildCreditCard(request.Card)
            };

        private CieloPaymentRequest BuildRecurrentCreditCardPayment(
            PaymentProviderRequest request,
            CieloIntegrationConfig config,
            long amount)
        {
            var payment = BuildCreditCardPayment(request, config, amount);
            payment.Installments = 1;
            payment.RecurrentPayment = BuildRecurrentPayment(request, config);
            return payment;
        }

        private CieloRecurrentPaymentRequest BuildRecurrentPayment(PaymentProviderRequest request, CieloIntegrationConfig config)
        {
            var recurrence = request.Recurrence;

            return new CieloRecurrentPaymentRequest
            {
                AuthorizeNow = recurrence?.ChargeImmediately ?? true,
                StartDate = FormatDate(recurrence?.StartDate),
                EndDate = FormatDate(recurrence?.EndDate),
                Interval = ResolveInterval(recurrence?.Interval, config).ToString()
            };
        }

        private CieloRecurrenceIntervalEnum ResolveInterval(
            PaymentRecurrenceIntervalEnum? interval,
            CieloIntegrationConfig config)
        {
            if (interval.HasValue)
                return MapInterval(interval.Value);

            return Enum.TryParse<CieloRecurrenceIntervalEnum>(config.RecurrenceInterval, ignoreCase: true, out var parsed)
                ? parsed
                : _options.RecurrenceInterval;
        }

        private static CieloRecurrenceIntervalEnum MapInterval(PaymentRecurrenceIntervalEnum interval) =>
            interval switch
            {
                PaymentRecurrenceIntervalEnum.Bimonthly => CieloRecurrenceIntervalEnum.Bimonthly,
                PaymentRecurrenceIntervalEnum.Quarterly => CieloRecurrenceIntervalEnum.Quarterly,
                PaymentRecurrenceIntervalEnum.SemiAnnual => CieloRecurrenceIntervalEnum.SemiAnnual,
                PaymentRecurrenceIntervalEnum.Annual => CieloRecurrenceIntervalEnum.Annual,
                _ => CieloRecurrenceIntervalEnum.Monthly
            };

        private static CieloPaymentRequest BuildPixPayment(PaymentProviderRequest request, long amount) =>
            new()
            {
                Type = CieloPaymentTypes.Pix,
                Amount = amount,
                Currency = request.Currency
            };

        private CieloPaymentRequest BuildBoletoPayment(
            PaymentProviderRequest request,
            CieloIntegrationConfig config,
            long amount)
        {
            var expirationDays = config.BoletoExpirationDays ?? _options.BoletoExpirationDays;
            var expiresAt = request.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(expirationDays);

            return new CieloPaymentRequest
            {
                Type = CieloPaymentTypes.Boleto,
                Amount = amount,
                Currency = request.Currency,
                Provider = Read(request.Metadata, CieloMetadataKeys.BoletoProvider)
                    ?? config.BoletoProvider
                    ?? _options.BoletoProvider,
                ExpirationDate = expiresAt.ToString(DateFormat, CultureInfo.InvariantCulture),
                Assignor = Read(request.Metadata, CieloMetadataKeys.BoletoAssignor)
                    ?? config.BoletoAssignor
                    ?? _options.BoletoAssignor,
                Instructions = Read(request.Metadata, CieloMetadataKeys.BoletoInstructions)
                    ?? config.BoletoInstructions
                    ?? _options.BoletoInstructions,
                Demonstrative = Read(request.Metadata, CieloMetadataKeys.BoletoDemonstrative)
                    ?? config.BoletoDemonstrative
                    ?? _options.BoletoDemonstrative,
                Identification = Read(request.Metadata, CieloMetadataKeys.BoletoIdentification)
                    ?? OnlyDigits(request.Customer.Document),
                BoletoNumber = Read(request.Metadata, CieloMetadataKeys.BoletoNumber)
            };
        }

        private static CieloCreditCardRequest? BuildCreditCard(PaymentCard? card)
        {
            if (card is null)
                return null;

            // O token de uso unico ja carrega numero, validade e CVV: reenviar esses dados
            // faria a Cielo recusar a autorizacao.
            if (!string.IsNullOrWhiteSpace(card.SingleUseToken))
                return new CieloCreditCardRequest
                {
                    PaymentToken = card.SingleUseToken,
                    Brand = card.Brand
                };

            return new CieloCreditCardRequest
            {
                CardToken = card.Token,
                CardNumber = OnlyDigits(card.Number),
                Holder = card.Holder,
                ExpirationDate = card.ExpirationDate,
                SecurityCode = card.SecurityCode,
                Brand = card.Brand,
                SaveCard = card.SaveCard ? true : null
            };
        }

        private static CieloCustomer BuildCustomer(PaymentProviderRequest request)
        {
            var customer = request.Customer;
            var document = OnlyDigits(customer.Document);

            return new CieloCustomer
            {
                Name = customer.Name,
                Email = customer.Email,
                Identity = document,
                IdentityType = document?.Length switch
                {
                    11 => "CPF",
                    14 => "CNPJ",
                    _ => null
                },
                Birthdate = FormatDate(customer.Birthdate),
                Address = BuildAddress(customer.Address)
            };
        }

        private static CieloAddress? BuildAddress(PaymentAddress? address)
        {
            if (address is null)
                return null;

            var zipCode = OnlyDigits(address.ZipCode);
            if (string.IsNullOrWhiteSpace(address.Street) && string.IsNullOrWhiteSpace(zipCode))
                return null;

            return new CieloAddress
            {
                Street = address.Street,
                Number = address.Number,
                Complement = address.Complement,
                District = address.District,
                ZipCode = zipCode,
                City = address.City,
                State = address.State,
                Country = address.Country ?? "BRA"
            };
        }

        private string? ResolveSoftDescriptor(IReadOnlyDictionary<string, string>? metadata, CieloIntegrationConfig config)
        {
            var descriptor = Read(metadata, CieloMetadataKeys.SoftDescriptor)
                ?? config.SoftDescriptor
                ?? _options.SoftDescriptor;

            if (string.IsNullOrWhiteSpace(descriptor))
                return null;

            return descriptor.Length > SoftDescriptorMaxLength
                ? descriptor[..SoftDescriptorMaxLength]
                : descriptor;
        }

        private static string BuildMerchantOrderId(PaymentProviderRequest request)
        {
            var reference = Sanitize(request.Reference);
            if (reference.Length == 0)
                reference = Sanitize(request.IdempotencyKey);

            return reference.Length > MerchantOrderIdMaxLength
                ? reference[..MerchantOrderIdMaxLength]
                : reference;
        }

        private CieloCredentials? ResolveCredentials(PaymentProviderContext? context, CieloIntegrationConfig config)
        {
            var credentials = ResolveNamedCredentials(context?.SecretReference)
                ?? BuildCredentials(config, context?.SecretReference)
                ?? _options.DefaultCredentials;

            if (credentials is null ||
                string.IsNullOrWhiteSpace(credentials.MerchantId) ||
                string.IsNullOrWhiteSpace(credentials.MerchantKey))
                return null;

            var environment = ResolveEnvironment(context?.Environment) ?? credentials.Environment;

            return environment == credentials.Environment
                ? credentials
                : new CieloCredentials
                {
                    MerchantId = credentials.MerchantId,
                    MerchantKey = credentials.MerchantKey,
                    Environment = environment
                };
        }

        private CieloCredentials? ResolveNamedCredentials(string? secretReference) =>
            !string.IsNullOrWhiteSpace(secretReference) &&
            _options.NamedCredentials.TryGetValue(secretReference.Trim(), out var credentials)
                ? credentials
                : null;

        private static CieloCredentials? BuildCredentials(CieloIntegrationConfig config, string? secretReference)
        {
            var merchantKey = config.MerchantKey ?? secretReference?.Trim();

            return string.IsNullOrWhiteSpace(config.MerchantId) || string.IsNullOrWhiteSpace(merchantKey)
                ? null
                : new CieloCredentials { MerchantId = config.MerchantId, MerchantKey = merchantKey };
        }

        private static CieloEnvironmentEnum? ResolveEnvironment(string? environment) =>
            environment?.Trim().ToLowerInvariant() switch
            {
                "production" or "producao" or "prod" or "live" => CieloEnvironmentEnum.Production,
                "sandbox" or "homologacao" or "development" or "test" => CieloEnvironmentEnum.Sandbox,
                _ => null
            };

        /// <summary>
        /// Na criacao, um pagamento negado e uma falha da cobranca.
        /// </summary>
        private static PaymentProviderResult MapCreatedSale(CieloSaleResponse sale)
        {
            var status = MapStatus(sale.Payment.Status, sale.Payment.Type);
            return MapSale(sale, status, status != PaymentProviderStatusEnum.Failed);
        }

        /// <summary>
        /// Na consulta, o sucesso e da propria consulta: o status recusado precisa
        /// chegar ao consumidor para atualizar a transacao.
        /// </summary>
        private static PaymentProviderResult MapQueriedSale(CieloSaleResponse sale) =>
            MapSale(sale, MapStatus(sale.Payment.Status, sale.Payment.Type), isSuccess: true);

        private static PaymentProviderResult MapSale(
            CieloSaleResponse sale,
            PaymentProviderStatusEnum status,
            bool isSuccess)
        {
            var payment = sale.Payment;

            return new PaymentProviderResult(
                IsSuccess: isSuccess,
                Status: status,
                TransactionId: payment.PaymentId,
                Reference: sale.MerchantOrderId,
                PaymentUrl: BuildUri(payment.Url),
                QrCode: payment.QrCodeString,
                Barcode: payment.DigitableLine ?? payment.BarCodeNumber,
                Amount: FromCents(payment.Amount),
                Currency: payment.Currency,
                ExpiresAt: ParseDate(payment.ExpirationDate),
                Message: payment.ReturnMessage ?? payment.ReasonMessage,
                RawPayload: sale.RawPayload,
                Metadata: BuildResultMetadata(payment));
        }

        private static PaymentProviderResult MapOperation(
            CieloOperationResponse operation,
            string transactionId,
            PaymentProviderStatusEnum expectedStatus)
        {
            var status = operation.Status == 0 ? expectedStatus : MapStatus(operation.Status, type: null);

            return new PaymentProviderResult(
                IsSuccess: status != PaymentProviderStatusEnum.Failed,
                Status: status,
                TransactionId: transactionId,
                Message: operation.ReturnMessage ?? operation.ReasonMessage,
                RawPayload: operation.RawPayload,
                Metadata: new Dictionary<string, string>
                {
                    ["status_code"] = operation.Status.ToString(CultureInfo.InvariantCulture),
                    ["return_code"] = operation.ReturnCode ?? string.Empty
                });
        }

        private static Dictionary<string, string> BuildResultMetadata(CieloPaymentResponse payment)
        {
            var metadata = new Dictionary<string, string>
            {
                ["status_code"] = payment.Status.ToString(CultureInfo.InvariantCulture)
            };

            AddWhenFilled(metadata, "payment_type", payment.Type);
            AddWhenFilled(metadata, "tid", payment.Tid);
            AddWhenFilled(metadata, "proof_of_sale", payment.ProofOfSale);
            AddWhenFilled(metadata, "authorization_code", payment.AuthorizationCode);
            AddWhenFilled(metadata, "return_code", payment.ReturnCode);
            AddWhenFilled(metadata, "bar_code_number", payment.BarCodeNumber);
            AddWhenFilled(metadata, "boleto_number", payment.BoletoNumber);
            AddWhenFilled(metadata, "qr_code_base64", payment.QrCodeBase64Image);
            AddWhenFilled(metadata, "card_token", payment.CreditCard?.CardToken);
            AddWhenFilled(metadata, "recurrent_payment_id", payment.RecurrentPayment?.RecurrentPaymentId);
            AddWhenFilled(metadata, "next_recurrency", payment.RecurrentPayment?.NextRecurrency);

            return metadata;
        }

        private static void AddWhenFilled(Dictionary<string, string> metadata, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                metadata[key] = value;
        }

        private static PaymentProviderStatusEnum MapStatus(int status, string? type)
        {
            var isBoleto = string.Equals(type, CieloPaymentTypes.Boleto, StringComparison.OrdinalIgnoreCase);

            return (CieloPaymentStatusEnum)status switch
            {
                CieloPaymentStatusEnum.Authorized when isBoleto => PaymentProviderStatusEnum.Pending,
                CieloPaymentStatusEnum.Authorized => PaymentProviderStatusEnum.Authorized,
                CieloPaymentStatusEnum.PaymentConfirmed => PaymentProviderStatusEnum.Paid,
                CieloPaymentStatusEnum.Denied => PaymentProviderStatusEnum.Failed,
                CieloPaymentStatusEnum.Voided => PaymentProviderStatusEnum.Cancelled,
                CieloPaymentStatusEnum.Refunded => PaymentProviderStatusEnum.Refunded,
                CieloPaymentStatusEnum.Aborted => PaymentProviderStatusEnum.Failed,
                _ => PaymentProviderStatusEnum.Pending
            };
        }

        private static string DescribeEvent(int changeType, PaymentProviderStatusEnum status) =>
            (CieloChangeTypeEnum)changeType switch
            {
                CieloChangeTypeEnum.RecurrentOrderCreated => "recurrent.order.created",
                CieloChangeTypeEnum.AntifraudStatusChanged => "antifraud.status.changed",
                CieloChangeTypeEnum.RecurrentPaymentStatusChanged => "recurrent.payment.changed",
                CieloChangeTypeEnum.CancellationDenied => "payment.cancellation.denied",
                CieloChangeTypeEnum.UnderpaidBoleto => "boleto.underpaid",
                CieloChangeTypeEnum.Chargeback => "payment.chargeback",
                CieloChangeTypeEnum.FraudAlert => "payment.fraud.alert",
                CieloChangeTypeEnum.PartialCancellation => "payment.partial.cancellation",
                _ => $"payment.{status.ToString().ToLowerInvariant()}"
            };

        private static PaymentProviderResult Failure(string message) =>
            new(false, PaymentProviderStatusEnum.Failed, Message: message);

        private static long ToCents(decimal amount) =>
            (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

        private static decimal FromCents(long amount) => amount / 100m;

        private static Uri? BuildUri(string? url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null;

        // A Cielo devolve emissao e expiracao da sessao sem fuso. A diferenca entre as duas
        // e o unico dado confiavel, entao a validade e projetada a partir de agora.
        private static DateTimeOffset? ParseExpiration(string? issued, string? expiresIn)
        {
            if (!DateTime.TryParse(issued, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                !DateTime.TryParse(expiresIn, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end) ||
                end <= start)
                return null;

            return DateTimeOffset.UtcNow.Add(end - start);
        }

        private static string? FormatDate(DateOnly? value) =>
            value?.ToString(DateFormat, CultureInfo.InvariantCulture);

        private static DateTimeOffset? ParseDate(string? value) =>
            DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? new DateTimeOffset(date, TimeSpan.Zero)
                : null;

        private static DateTimeOffset ParseReceivedDate(string? value) =>
            DateTime.TryParseExact(value, ReceivedDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? new DateTimeOffset(date, TimeSpan.Zero)
                : DateTimeOffset.UtcNow;

        private static string? Read(IReadOnlyDictionary<string, string>? metadata, string key)
        {
            if (metadata is null)
                return null;

            foreach (var item in metadata)
            {
                if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(item.Value))
                    return item.Value.Trim();
            }

            return null;
        }

        private static bool? ReadBoolean(IReadOnlyDictionary<string, string>? metadata, string key) =>
            bool.TryParse(Read(metadata, key), out var value) ? value : null;

        private static string? OnlyDigits(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var digits = new string(value.Where(char.IsDigit).ToArray());
            return digits.Length == 0 ? null : digits;
        }

        private static string Sanitize(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : new string(value.Where(char.IsLetterOrDigit).ToArray());

        private const string MissingCredentialsMessage =
            "Credenciais da Cielo nao foram configuradas para a integracao.";
    }
}
