using JotaSystem.Sdk.Core.CrossCutting.Providers.Enum;
using JotaSystem.Sdk.Core.CrossCutting.Providers.Models;
using JotaSystem.Sdk.Providers.Payments.Cielo.Link;
using JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models;
using JotaSystem.Sdk.Providers.Payments.Cielo.Models;
using System.Globalization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Adapta as APIs da Cielo ao contrato de gateway de pagamento do SDK: a API E-commerce
    /// atende cartao, Pix e boleto e a API Link de Pagamento atende o meio
    /// <see cref="CieloMethodCodes.PaymentLink"/>.
    /// </summary>
    internal sealed class CieloPaymentGatewayProvider(
        ICieloProvider cieloProvider,
        ICieloLinkProvider cieloLinkProvider,
        CieloOptions options)
        : IPaymentGatewayProvider
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string ReceivedDateFormat = "yyyy-MM-dd HH:mm:ss";
        private const int SoftDescriptorMaxLength = 13;
        private const int MerchantOrderIdMaxLength = 50;
        private const int LinkNameMaxLength = 128;
        private const int LinkOrderNumberMaxLength = 20;
        private const int LinkMaxInstallments = 18;

        private readonly ICieloProvider _cieloProvider = cieloProvider;
        private readonly ICieloLinkProvider _cieloLinkProvider = cieloLinkProvider;
        private readonly CieloOptions _options = options;

        public string ProviderKey => "cielo";

        public IReadOnlyList<PaymentMethodOption> SupportedMethods { get; } =
        [
            new(CieloMethodCodes.CreditCard, "Cartão de crédito",
                "Autorização à vista ou parcelada, com captura automática.", RequiresCard: true),
            new(CieloMethodCodes.RecurrentCreditCard, "Cartão de crédito (recorrente)",
                "A Cielo agenda e repete a cobrança na periodicidade contratada.", RequiresCard: true),
            new(CieloMethodCodes.Pix, "Pix",
                "QR Code com confirmação automática por notificação."),
            new(CieloMethodCodes.Boleto, "Boleto",
                "Boleto registrado no banco emissor configurado na integração."),
            new(CieloMethodCodes.PaymentLink, "Link de pagamento",
                "Página hospedada pela Cielo: o cliente escolhe o meio e informa o cartão.")
        ];

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

            if (!CieloMethodCodes.TryResolve(request.MethodCode, out var method))
                return Failure($"O metodo de pagamento '{request.MethodCode}' nao e suportado pela Cielo.");

            if (method == CieloPaymentMethodEnum.PaymentLink)
                return await CreatePaymentLinkAsync(request, config, cancellationToken);

            var credentials = ResolveCredentials(request.Context, config);
            if (credentials is null)
                return Failure(MissingCredentialsMessage);

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

            if (IsPaymentLink(query.MethodCode))
                return await GetPaymentLinkAsync(query, config, cancellationToken);

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

            if (IsPaymentLink(operation.MethodCode))
                return await CancelPaymentLinkAsync(
                    operation.Context,
                    config,
                    operation.TransactionId,
                    amount: null,
                    PaymentProviderStatusEnum.Cancelled,
                    cancellationToken);

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

            if (IsPaymentLink(operation.MethodCode))
                return await CancelPaymentLinkAsync(
                    operation.Context,
                    config,
                    operation.TransactionId,
                    operation.Amount.HasValue ? ToCents(operation.Amount.Value) : null,
                    PaymentProviderStatusEnum.Refunded,
                    cancellationToken);

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

            // As duas APIs postam na mesma URL: o Link de Pagamento manda o pedido do
            // Checkout Cielo e a API E-commerce manda o PaymentId da transacao.
            var linkNotification = CieloLinkNotification.TryParse(request.Payload);
            if (linkNotification is not null)
                return await ParseLinkWebhookAsync(request, config, linkNotification, cancellationToken);

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

        private async Task<PaymentProviderResult> CreatePaymentLinkAsync(
            PaymentProviderRequest request,
            CieloIntegrationConfig config,
            CancellationToken cancellationToken)
        {
            var credentials = ResolveLinkCredentials(request.Context, config);
            if (credentials is null)
                return Failure(MissingLinkCredentialsMessage);

            var response = await _cieloLinkProvider.CreateLinkAsync(
                BuildLink(request, config),
                credentials,
                cancellationToken);

            return response.Success
                ? MapCreatedLink(response.Data!)
                : Failure(response.ErrorMessage!);
        }

        /// <summary>
        /// O link so vira transacao quando alguem paga. Enquanto nao ha pedido, a cobranca
        /// segue pendente e o que interessa devolver e a propria URL de pagamento.
        /// </summary>
        private async Task<PaymentProviderResult> GetPaymentLinkAsync(
            PaymentProviderQuery query,
            CieloIntegrationConfig config,
            CancellationToken cancellationToken)
        {
            var credentials = ResolveLinkCredentials(query.Context, config);
            if (credentials is null)
                return Failure(MissingLinkCredentialsMessage);

            var orders = await _cieloLinkProvider.GetLinkOrdersAsync(
                query.TransactionId,
                credentials,
                cancellationToken);
            if (!orders.Success)
                return Failure(orders.ErrorMessage!);

            var order = SelectRelevantOrder(orders.Data!);
            if (order is not null)
                return MapLinkOrder(query.TransactionId, order, orders.Data!.RawPayload, isSuccess: true);

            var link = await _cieloLinkProvider.GetLinkAsync(query.TransactionId, credentials, cancellationToken);

            return link.Success
                ? MapCreatedLink(link.Data!)
                : Failure(link.ErrorMessage!);
        }

        /// <summary>
        /// Sem pedido pago, cancelar e apagar o link para que ninguem mais consiga paga-lo.
        /// Com pedido, o cancelamento vai para o pedido do Checkout Cielo.
        /// </summary>
        private async Task<PaymentProviderResult> CancelPaymentLinkAsync(
            PaymentProviderContext? context,
            CieloIntegrationConfig config,
            string linkId,
            long? amount,
            PaymentProviderStatusEnum expectedStatus,
            CancellationToken cancellationToken)
        {
            var credentials = ResolveLinkCredentials(context, config);
            if (credentials is null)
                return Failure(MissingLinkCredentialsMessage);

            var orders = await _cieloLinkProvider.GetLinkOrdersAsync(linkId, credentials, cancellationToken);
            if (!orders.Success)
                return Failure(orders.ErrorMessage!);

            var order = SelectRelevantOrder(orders.Data!);
            var checkoutOrderNumber = ResolveCheckoutOrderNumber(order);

            if (checkoutOrderNumber is null)
            {
                var deleted = await _cieloLinkProvider.DeleteLinkAsync(linkId, credentials, cancellationToken);

                return deleted.Success
                    ? new PaymentProviderResult(
                        IsSuccess: true,
                        Status: PaymentProviderStatusEnum.Cancelled,
                        TransactionId: linkId,
                        Message: "Link de pagamento cancelado antes de qualquer pagamento.")
                    : Failure(deleted.ErrorMessage!);
            }

            var response = await _cieloLinkProvider.VoidOrderAsync(
                checkoutOrderNumber,
                amount,
                credentials,
                cancellationToken);
            if (!response.Success)
                return Failure(response.ErrorMessage!);

            var operation = response.Data!;

            return new PaymentProviderResult(
                IsSuccess: true,
                Status: expectedStatus,
                TransactionId: linkId,
                Reference: checkoutOrderNumber,
                Message: operation.ReturnMessage,
                RawPayload: operation.RawPayload,
                Metadata: new Dictionary<string, string>
                {
                    [CieloLinkMetadata.CheckoutOrderNumber] = checkoutOrderNumber,
                    ["return_code"] = operation.ReturnCode ?? string.Empty
                });
        }

        /// <summary>
        /// A notificacao do Link nao e assinada, entao o status e confirmado na consulta do
        /// pedido sempre que ela responde. Quando a consulta falha, vale o que a Cielo postou.
        /// </summary>
        private async Task<PaymentWebhookEvent> ParseLinkWebhookAsync(
            PaymentWebhookRequest request,
            CieloIntegrationConfig config,
            CieloLinkNotification notification,
            CancellationToken cancellationToken)
        {
            EnsureWebhookIsTrusted(request, config);

            var linkId = notification.ProductId
                ?? throw new InvalidOperationException("Notificacao do Link de Pagamento sem o identificador do link.");
            var credentials = ResolveLinkCredentials(request.Context, config);
            var confirmed = await ConfirmLinkOrderAsync(notification, credentials, cancellationToken);

            var status = MapLinkStatus(confirmed?.Payment?.Status ?? notification.PaymentStatus);
            var amount = confirmed?.Payment?.Price ?? notification.Amount;
            var metadata = BuildLinkMetadata(notification, confirmed);

            return new PaymentWebhookEvent(
                ProviderKey,
                $"{notification.CheckoutCieloOrderNumber ?? linkId}-{status}",
                $"payment.{status.ToString().ToLowerInvariant()}",
                linkId,
                status,
                amount.HasValue ? FromCents(amount.Value) : null,
                Currency: null,
                notification.CreatedDate ?? DateTimeOffset.UtcNow,
                confirmed?.RawPayload ?? request.Payload,
                metadata);
        }

        private async Task<CieloLinkOrder?> ConfirmLinkOrderAsync(
            CieloLinkNotification notification,
            CieloLinkCredentials? credentials,
            CancellationToken cancellationToken)
        {
            if (credentials is null || string.IsNullOrWhiteSpace(notification.CheckoutCieloOrderNumber))
                return null;

            var response = await _cieloLinkProvider.GetOrderAsync(
                notification.CheckoutCieloOrderNumber,
                credentials,
                cancellationToken);

            return response.Success ? response.Data : null;
        }

        private CieloLinkRequest BuildLink(PaymentProviderRequest request, CieloIntegrationConfig config)
        {
            var recurrence = request.Recurrence;
            var type = recurrence is not null
                ? CieloLinkProductTypes.Recurrent
                : Read(request.Metadata, CieloMetadataKeys.LinkProductType)
                    ?? config.LinkProductType
                    ?? _options.LinkProductType;

            return new CieloLinkRequest
            {
                OrderNumber = BuildLinkOrderNumber(request),
                Type = type,
                Name = BuildLinkName(request),
                Description = Read(request.Metadata, CieloMetadataKeys.LinkDescription),
                Price = ToCents(request.Amount),
                ExpirationDate = FormatLinkExpiration(request, config),
                SoftDescriptor = ResolveSoftDescriptor(request.Metadata, config),
                MaxNumberOfInstallments = recurrence is null ? ResolveLinkInstallments(request, config) : null,
                Quantity = 1,
                Sku = Read(request.Metadata, CieloMetadataKeys.LinkSku),
                Shipping = new CieloLinkShipping
                {
                    Type = Read(request.Metadata, CieloMetadataKeys.LinkShippingType)
                        ?? config.LinkShippingType
                        ?? _options.LinkShippingType
                },
                Recurrent = recurrence is null
                    ? null
                    : new CieloLinkRecurrent
                    {
                        Interval = ResolveInterval(recurrence.Interval, config).ToString(),
                        EndDate = FormatDate(recurrence.EndDate)
                    },
                CustomLinkConfiguration = BuildLinkConfiguration(request, config)
            };
        }

        private CieloLinkCustomConfiguration? BuildLinkConfiguration(
            PaymentProviderRequest request,
            CieloIntegrationConfig config)
        {
            var configured = Read(request.Metadata, CieloMetadataKeys.LinkPaymentTypes) ?? config.LinkPaymentTypes;
            var paymentTypes = configured is null
                ? [.. _options.LinkPaymentTypes]
                : configured
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

            return paymentTypes.Count == 0
                ? null
                : new CieloLinkCustomConfiguration { PaymentTypes = paymentTypes };
        }

        private int? ResolveLinkInstallments(PaymentProviderRequest request, CieloIntegrationConfig config)
        {
            var installments = request.InstallmentCount > 1
                ? request.InstallmentCount
                : int.TryParse(Read(request.Metadata, CieloMetadataKeys.LinkMaxInstallments), out var configured)
                    ? configured
                    : config.LinkMaxInstallments ?? _options.LinkMaxInstallments;

            if (installments is null or <= 1)
                return null;

            return installments > LinkMaxInstallments ? LinkMaxInstallments : installments;
        }

        private string? FormatLinkExpiration(PaymentProviderRequest request, CieloIntegrationConfig config)
        {
            if (request.ExpiresAt.HasValue)
                return request.ExpiresAt.Value.ToString(DateFormat, CultureInfo.InvariantCulture);

            var days = config.LinkExpirationDays ?? _options.LinkExpirationDays;

            return days is null or <= 0
                ? null
                : DateTimeOffset.UtcNow.AddDays(days.Value).ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        private static string BuildLinkName(PaymentProviderRequest request)
        {
            var name = Read(request.Metadata, CieloMetadataKeys.LinkName)
                ?? (string.IsNullOrWhiteSpace(request.Reference) ? "Cobranca" : $"Pedido {request.Reference.Trim()}");

            return name.Length > LinkNameMaxLength ? name[..LinkNameMaxLength] : name;
        }

        private static string BuildLinkOrderNumber(PaymentProviderRequest request)
        {
            var reference = Sanitize(request.Reference);
            if (reference.Length == 0)
                reference = Sanitize(request.IdempotencyKey);

            return reference.Length > LinkOrderNumberMaxLength
                ? reference[..LinkOrderNumberMaxLength]
                : reference;
        }

        /// <summary>
        /// Um link aceita mais de um pedido. Um pagamento concluido vale mais que uma
        /// tentativa recusada, e entre iguais vale o mais recente.
        /// </summary>
        private static CieloLinkOrder? SelectRelevantOrder(CieloLinkOrderList orders) =>
            orders.Orders
                .OrderByDescending(x => RankLinkStatus(x.Payment?.Status))
                .ThenByDescending(x => x.CreatedDate, StringComparer.Ordinal)
                .FirstOrDefault();

        private static int RankLinkStatus(CieloLinkStatusEnum? status) =>
            status switch
            {
                CieloLinkStatusEnum.Paid => 5,
                CieloLinkStatusEnum.Authorized => 4,
                CieloLinkStatusEnum.AuthorizedIdPayPending => 3,
                CieloLinkStatusEnum.Pending => 2,
                null => 0,
                _ => 1
            };

        // A listagem por link e a consulta por pedido nomeiam o identificador do Checkout
        // Cielo de formas diferentes, entao as duas leituras caem aqui.
        private static string? ResolveCheckoutOrderNumber(CieloLinkOrder? order) =>
            string.IsNullOrWhiteSpace(order?.CheckoutCieloOrderNumber)
                ? (string.IsNullOrWhiteSpace(order?.OrderNumber) ? null : order.OrderNumber.Trim())
                : order.CheckoutCieloOrderNumber.Trim();

        private static PaymentProviderResult MapCreatedLink(CieloLinkResponse link)
        {
            var metadata = new Dictionary<string, string>();
            AddWhenFilled(metadata, CieloLinkMetadata.LinkId, link.Id);
            AddWhenFilled(metadata, CieloLinkMetadata.ShortUrl, link.ShortUrl);
            AddWhenFilled(metadata, "link_type", link.Type);

            return new PaymentProviderResult(
                IsSuccess: true,
                Status: PaymentProviderStatusEnum.Pending,
                TransactionId: link.Id,
                Reference: link.OrderNumber,
                PaymentUrl: BuildUri(link.ShortUrl),
                Amount: link.Price.HasValue ? FromCents(link.Price.Value) : null,
                ExpiresAt: ParseLinkDate(link.ExpirationDate),
                Message: "Link de pagamento gerado. Envie a URL para o cliente concluir o pagamento.",
                RawPayload: link.RawPayload,
                Metadata: metadata);
        }

        private static PaymentProviderResult MapLinkOrder(
            string linkId,
            CieloLinkOrder order,
            string? rawPayload,
            bool isSuccess)
        {
            var payment = order.Payment;
            var metadata = new Dictionary<string, string>();
            AddWhenFilled(metadata, CieloLinkMetadata.LinkId, linkId);
            AddWhenFilled(metadata, CieloLinkMetadata.CheckoutOrderNumber, ResolveCheckoutOrderNumber(order));
            AddWhenFilled(metadata, "payment_method_type", payment?.PaymentMethodType);
            AddWhenFilled(metadata, "tid", payment?.Tid);
            AddWhenFilled(metadata, "authorization_code", payment?.AuthorizationCode);
            AddWhenFilled(metadata, "boleto_number", payment?.BoletoNumber);

            return new PaymentProviderResult(
                IsSuccess: isSuccess,
                Status: MapLinkStatus(payment?.Status),
                TransactionId: linkId,
                Reference: ResolveCheckoutOrderNumber(order),
                QrCode: payment?.QrCodeUrl,
                Barcode: payment?.BoletoNumber,
                Amount: payment?.Price.HasValue == true ? FromCents(payment.Price!.Value) : null,
                RawPayload: rawPayload,
                Metadata: metadata);
        }

        private static Dictionary<string, string> BuildLinkMetadata(
            CieloLinkNotification notification,
            CieloLinkOrder? confirmed)
        {
            var metadata = new Dictionary<string, string>();
            AddWhenFilled(metadata, CieloLinkMetadata.CheckoutOrderNumber, notification.CheckoutCieloOrderNumber);
            AddWhenFilled(metadata, CieloLinkMetadata.LinkId, notification.ProductId);
            AddWhenFilled(metadata, "order_number", notification.OrderNumber);
            AddWhenFilled(metadata, "payment_method_type", notification.PaymentMethodType);
            AddWhenFilled(metadata, "tid", notification.Tid ?? confirmed?.Payment?.Tid);
            AddWhenFilled(metadata, "boleto_number", notification.BoletoNumber);
            AddWhenFilled(metadata, "end_to_end_id", notification.EndToEndId);
            AddWhenFilled(metadata, "recurrent_payment_id", notification.RecurrentPaymentId);
            AddWhenFilled(metadata, "confirmed_by_query", confirmed is null ? "false" : "true");

            if (notification.IsTest)
                metadata["test_transaction"] = "true";

            return metadata;
        }

        private static PaymentProviderStatusEnum MapLinkStatus(CieloLinkStatusEnum? status) =>
            status switch
            {
                CieloLinkStatusEnum.Paid => PaymentProviderStatusEnum.Paid,
                CieloLinkStatusEnum.Authorized => PaymentProviderStatusEnum.Authorized,
                CieloLinkStatusEnum.Denied => PaymentProviderStatusEnum.Failed,
                CieloLinkStatusEnum.NotFinalized => PaymentProviderStatusEnum.Failed,
                CieloLinkStatusEnum.Expired => PaymentProviderStatusEnum.Expired,
                CieloLinkStatusEnum.Voided => PaymentProviderStatusEnum.Cancelled,
                _ => PaymentProviderStatusEnum.Pending
            };

        private static bool IsPaymentLink(string? methodCode) =>
            CieloMethodCodes.TryResolve(methodCode, out var method) &&
            method == CieloPaymentMethodEnum.PaymentLink;

        /// <summary>
        /// Credenciais da API Link de Pagamento: o que a integracao do tenant informa tem
        /// prioridade e o que faltar vem da configuracao padrao da aplicacao.
        /// </summary>
        private CieloLinkCredentials? ResolveLinkCredentials(
            PaymentProviderContext? context,
            CieloIntegrationConfig config)
        {
            var secrets = context?.Secrets;
            var fallback = _options.DefaultLinkCredentials;

            var clientId = FirstFilled(
                Read(secrets, CieloSecretKeys.LinkClientId),
                config.LinkClientId,
                fallback?.ClientId);
            var clientSecret = FirstFilled(
                Read(secrets, CieloSecretKeys.LinkClientSecret),
                fallback?.ClientSecret);

            if (clientId is null || clientSecret is null)
                return null;

            return new CieloLinkCredentials { ClientId = clientId, ClientSecret = clientSecret };
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

        /// <summary>
        /// Monta as credenciais campo a campo: o que a integracao do tenant informa tem
        /// prioridade e o que faltar vem da configuracao padrao da aplicacao.
        /// </summary>
        private CieloCredentials? ResolveCredentials(PaymentProviderContext? context, CieloIntegrationConfig config)
        {
            var secrets = context?.Secrets;
            var named = ResolveNamedCredentials(context?.SecretReference);
            var fallback = named ?? _options.DefaultCredentials;

            // A SecretReference so vale como MerchantKey quando nao aponta para uma credencial nomeada.
            var legacyMerchantKey = named is null ? context?.SecretReference : null;

            var merchantId = FirstFilled(Read(secrets, CieloSecretKeys.MerchantId), config.MerchantId, fallback?.MerchantId);
            var merchantKey = FirstFilled(Read(secrets, CieloSecretKeys.MerchantKey), config.MerchantKey, legacyMerchantKey, fallback?.MerchantKey);

            if (merchantId is null || merchantKey is null)
                return null;

            return new CieloCredentials
            {
                MerchantId = merchantId,
                MerchantKey = merchantKey,
                ClientId = FirstFilled(Read(secrets, CieloSecretKeys.ClientId), config.ClientId, fallback?.ClientId),
                ClientSecret = FirstFilled(Read(secrets, CieloSecretKeys.ClientSecret), fallback?.ClientSecret),
                Environment = ResolveEnvironment(context?.Environment)
                    ?? fallback?.Environment
                    ?? CieloEnvironmentEnum.Sandbox
            };
        }

        private CieloCredentials? ResolveNamedCredentials(string? secretReference) =>
            !string.IsNullOrWhiteSpace(secretReference) &&
            _options.NamedCredentials.TryGetValue(secretReference.Trim(), out var credentials)
                ? credentials
                : null;

        private static string? FirstFilled(params string?[] values) =>
            Array.Find(values, x => !string.IsNullOrWhiteSpace(x))?.Trim();

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
                Metadata: BuildResultMetadata(payment),
                QrCodeImage: payment.QrCodeBase64Image);
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

        private const string MissingLinkCredentialsMessage =
            "Credenciais da API Link de Pagamento nao foram configuradas para a integracao.";

        private static DateTimeOffset? ParseLinkDate(string? value)
        {
            var formats = new[] { DateFormat, ReceivedDateFormat, $"{DateFormat}'T'HH:mm:ss" };

            return DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date)
                ? new DateTimeOffset(date, TimeSpan.Zero)
                : null;
        }
    }

    /// <summary>
    /// Chaves publicadas no <c>Metadata</c> das cobrancas criadas pelo Link de Pagamento.
    /// </summary>
    internal static class CieloLinkMetadata
    {
        internal const string LinkId = "link_id";
        internal const string ShortUrl = "link_short_url";
        internal const string CheckoutOrderNumber = "checkout_cielo_order_number";
    }
}
