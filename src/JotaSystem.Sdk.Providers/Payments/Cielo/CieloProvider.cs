using JotaSystem.Sdk.Providers.Payments.Cielo.Models;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    internal sealed class CieloProvider(IHttpClientFactory httpClientFactory, CieloOptions options) : ICieloProvider
    {
        private const string SalesPath = "1/sales";
        private const string RecurrentPaymentPath = "1/RecurrentPayment";
        private const string DateFormat = "yyyy-MM-dd";

        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly CieloOptions _options = options;

        public async Task<ApiResponse<CieloSaleResponse>> CreateSaleAsync(
            CieloSaleRequest request,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            var validationError = ValidateSaleRequest(request);
            if (validationError is not null)
                return ApiResponse<CieloSaleResponse>.CreateFail(validationError);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloSaleResponse>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(
                HttpMethod.Post,
                BuildUrl(TransactionUrl(selectedCredentials), $"{SalesPath}/"),
                selectedCredentials,
                request,
                cancellationToken);

            return ReadSale(response);
        }

        public async Task<ApiResponse<CieloSaleResponse>> GetSaleAsync(
            string paymentId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                return ApiResponse<CieloSaleResponse>.CreateFail("Informe o PaymentId da transacao.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloSaleResponse>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(
                HttpMethod.Get,
                BuildUrl(QueryUrl(selectedCredentials), $"{SalesPath}/{Uri.EscapeDataString(paymentId.Trim())}"),
                selectedCredentials,
                body: null,
                cancellationToken);

            return ReadSale(response);
        }

        public async Task<ApiResponse<List<CieloMerchantOrderPayment>>> GetSalesByMerchantOrderIdAsync(
            string merchantOrderId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(merchantOrderId))
                return ApiResponse<List<CieloMerchantOrderPayment>>.CreateFail("Informe o MerchantOrderId do pedido.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<List<CieloMerchantOrderPayment>>.CreateFail(MissingCredentialsMessage);

            var url = BuildUrl(
                QueryUrl(selectedCredentials),
                $"{SalesPath}?merchantOrderId={Uri.EscapeDataString(merchantOrderId.Trim())}");
            var response = await SendAsync(HttpMethod.Get, url, selectedCredentials, body: null, cancellationToken);

            if (!response.IsSuccess)
                return ApiResponse<List<CieloMerchantOrderPayment>>.CreateFail(response.ErrorMessage!);

            var data = Deserialize<CieloMerchantOrderResponse>(response.Content);
            return data is null
                ? ApiResponse<List<CieloMerchantOrderPayment>>.CreateFail(InvalidResponseMessage)
                : ApiResponse<List<CieloMerchantOrderPayment>>.CreateSuccess(data.Payments);
        }

        public async Task<ApiResponse<CieloOperationResponse>> CaptureAsync(
            string paymentId,
            long? amount = null,
            long? serviceTaxAmount = null,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                return ApiResponse<CieloOperationResponse>.CreateFail("Informe o PaymentId da transacao.");

            if (amount.HasValue && amount.Value <= 0)
                return ApiResponse<CieloOperationResponse>.CreateFail("O valor da captura deve ser maior que zero.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloOperationResponse>.CreateFail(MissingCredentialsMessage);

            var query = BuildAmountQuery(amount, serviceTaxAmount);
            var response = await SendAsync(
                HttpMethod.Put,
                BuildUrl(TransactionUrl(selectedCredentials), $"{SalesPath}/{Uri.EscapeDataString(paymentId.Trim())}/capture{query}"),
                selectedCredentials,
                body: null,
                cancellationToken);

            return ReadOperation(response);
        }

        public async Task<ApiResponse<CieloOperationResponse>> VoidAsync(
            string paymentId,
            long? amount = null,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                return ApiResponse<CieloOperationResponse>.CreateFail("Informe o PaymentId da transacao.");

            if (amount.HasValue && amount.Value <= 0)
                return ApiResponse<CieloOperationResponse>.CreateFail("O valor do cancelamento deve ser maior que zero.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloOperationResponse>.CreateFail(MissingCredentialsMessage);

            var query = BuildAmountQuery(amount, serviceTaxAmount: null);
            var response = await SendAsync(
                HttpMethod.Put,
                BuildUrl(TransactionUrl(selectedCredentials), $"{SalesPath}/{Uri.EscapeDataString(paymentId.Trim())}/void{query}"),
                selectedCredentials,
                body: null,
                cancellationToken);

            return ReadOperation(response);
        }

        public async Task<ApiResponse<CieloRecurrentPaymentResponse>> GetRecurrentPaymentAsync(
            string recurrentPaymentId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(recurrentPaymentId))
                return ApiResponse<CieloRecurrentPaymentResponse>.CreateFail("Informe o RecurrentPaymentId da recorrencia.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloRecurrentPaymentResponse>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(
                HttpMethod.Get,
                BuildUrl(QueryUrl(selectedCredentials), $"{RecurrentPaymentPath}/{Uri.EscapeDataString(recurrentPaymentId.Trim())}"),
                selectedCredentials,
                body: null,
                cancellationToken);

            if (!response.IsSuccess)
                return ApiResponse<CieloRecurrentPaymentResponse>.CreateFail(response.ErrorMessage!);

            var data = Deserialize<CieloRecurrentPaymentResponse>(response.Content);
            if (data is null)
                return ApiResponse<CieloRecurrentPaymentResponse>.CreateFail(InvalidResponseMessage);

            data.RawPayload = response.Content;
            return ApiResponse<CieloRecurrentPaymentResponse>.CreateSuccess(data);
        }

        public async Task<ApiResponse<bool>> UpdateRecurrentPaymentAsync(
            string recurrentPaymentId,
            CieloRecurrentPaymentUpdate update,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(recurrentPaymentId))
                return ApiResponse<bool>.CreateFail("Informe o RecurrentPaymentId da recorrencia.");

            if (update is null || !update.HasChanges)
                return ApiResponse<bool>.CreateFail("Informe ao menos um dado para alterar na recorrencia.");

            if (update.Amount.HasValue && update.Amount.Value <= 0)
                return ApiResponse<bool>.CreateFail("O valor da recorrencia deve ser maior que zero.");

            if (update.RecurrencyDay is < 1 or > 31)
                return ApiResponse<bool>.CreateFail("O dia da recorrencia deve estar entre 1 e 31.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<bool>.CreateFail(MissingCredentialsMessage);

            foreach (var change in BuildRecurrentPaymentChanges(update))
            {
                var result = await SendRecurrentPaymentCommandAsync(
                    recurrentPaymentId,
                    change.Resource,
                    change.Body,
                    selectedCredentials,
                    cancellationToken);

                if (!result.Success)
                    return result;
            }

            return ApiResponse<bool>.CreateSuccess(true);
        }

        public Task<ApiResponse<bool>> DeactivateRecurrentPaymentAsync(
            string recurrentPaymentId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default) =>
            ChangeRecurrentPaymentStateAsync(recurrentPaymentId, "Deactivate", credentials, cancellationToken);

        public Task<ApiResponse<bool>> ReactivateRecurrentPaymentAsync(
            string recurrentPaymentId,
            CieloCredentials? credentials = null,
            CancellationToken cancellationToken = default) =>
            ChangeRecurrentPaymentStateAsync(recurrentPaymentId, "Reactivate", credentials, cancellationToken);

        private async Task<ApiResponse<bool>> ChangeRecurrentPaymentStateAsync(
            string recurrentPaymentId,
            string resource,
            CieloCredentials? credentials,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(recurrentPaymentId))
                return ApiResponse<bool>.CreateFail("Informe o RecurrentPaymentId da recorrencia.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<bool>.CreateFail(MissingCredentialsMessage);

            return await SendRecurrentPaymentCommandAsync(
                recurrentPaymentId,
                resource,
                body: null,
                selectedCredentials,
                cancellationToken);
        }

        private async Task<ApiResponse<bool>> SendRecurrentPaymentCommandAsync(
            string recurrentPaymentId,
            string resource,
            object? body,
            CieloCredentials credentials,
            CancellationToken cancellationToken)
        {
            var url = BuildUrl(
                TransactionUrl(credentials),
                $"{RecurrentPaymentPath}/{Uri.EscapeDataString(recurrentPaymentId.Trim())}/{resource}");
            var response = await SendAsync(HttpMethod.Put, url, credentials, body, cancellationToken);

            return response.IsSuccess
                ? ApiResponse<bool>.CreateSuccess(true)
                : ApiResponse<bool>.CreateFail(response.ErrorMessage!);
        }

        private static IEnumerable<RecurrentPaymentChange> BuildRecurrentPaymentChanges(CieloRecurrentPaymentUpdate update)
        {
            if (update.Customer is not null)
                yield return new RecurrentPaymentChange("Customer", update.Customer);

            if (update.Payment is not null)
                yield return new RecurrentPaymentChange("Payment", update.Payment);

            if (update.Amount.HasValue)
                yield return new RecurrentPaymentChange("Amount", update.Amount.Value);

            if (update.RecurrencyDay.HasValue)
                yield return new RecurrentPaymentChange("RecurrencyDay", update.RecurrencyDay.Value);

            if (update.Interval.HasValue)
                yield return new RecurrentPaymentChange("Interval", (int)update.Interval.Value);

            if (update.NextPaymentDate.HasValue)
                yield return new RecurrentPaymentChange(
                    "NextPaymentDate",
                    update.NextPaymentDate.Value.ToString(DateFormat, CultureInfo.InvariantCulture));

            if (update.EndDate.HasValue)
                yield return new RecurrentPaymentChange(
                    "EndDate",
                    update.EndDate.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
        }

        private async Task<CieloHttpResponse> SendAsync(
            HttpMethod method,
            string url,
            CieloCredentials credentials,
            object? body,
            CancellationToken cancellationToken)
        {
            try
            {
                using var message = new HttpRequestMessage(method, url);
                message.Headers.TryAddWithoutValidation("MerchantId", credentials.MerchantId);
                message.Headers.TryAddWithoutValidation("MerchantKey", credentials.MerchantKey);
                message.Headers.TryAddWithoutValidation("RequestId", Guid.NewGuid().ToString());

                if (body is not null)
                    message.Content = new StringContent(
                        CieloJson.Serialize(body),
                        Encoding.UTF8,
                        "application/json");

                var client = _httpClientFactory.CreateClient(CieloHttpClientNames.Default);
                using var response = await client.SendAsync(message, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                return response.IsSuccessStatusCode
                    ? new CieloHttpResponse(true, content, null)
                    : new CieloHttpResponse(false, content, DescribeError(response.StatusCode, content));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new CieloHttpResponse(false, string.Empty, "Tempo limite excedido na comunicacao com a Cielo.");
            }
            catch (HttpRequestException ex)
            {
                return new CieloHttpResponse(false, string.Empty, $"Falha na comunicacao com a Cielo: {ex.Message}");
            }
        }

        private static ApiResponse<CieloSaleResponse> ReadSale(CieloHttpResponse response)
        {
            if (!response.IsSuccess)
                return ApiResponse<CieloSaleResponse>.CreateFail(response.ErrorMessage!);

            var data = Deserialize<CieloSaleResponse>(response.Content);
            if (data is null)
                return ApiResponse<CieloSaleResponse>.CreateFail(InvalidResponseMessage);

            data.RawPayload = response.Content;
            return ApiResponse<CieloSaleResponse>.CreateSuccess(data);
        }

        private static ApiResponse<CieloOperationResponse> ReadOperation(CieloHttpResponse response)
        {
            if (!response.IsSuccess)
                return ApiResponse<CieloOperationResponse>.CreateFail(response.ErrorMessage!);

            var data = Deserialize<CieloOperationResponse>(response.Content);
            if (data is null)
                return ApiResponse<CieloOperationResponse>.CreateFail(InvalidResponseMessage);

            data.RawPayload = response.Content;
            return ApiResponse<CieloOperationResponse>.CreateSuccess(data);
        }

        private static T? Deserialize<T>(string content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                return CieloJson.Deserialize<T>(content);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string DescribeError(HttpStatusCode statusCode, string content)
        {
            var errors = Deserialize<List<CieloError>>(content);
            if (errors is not null && errors.Count > 0)
                return string.Join(" | ", errors.Select(x => $"{x.Code} - {x.Message}"));

            return string.IsNullOrWhiteSpace(content)
                ? $"A Cielo respondeu com o status {(int)statusCode}."
                : $"A Cielo respondeu com o status {(int)statusCode}: {content}";
        }

        private CieloCredentials? ResolveCredentials(CieloCredentials? credentials)
        {
            var selected = credentials ?? _options.DefaultCredentials;

            return selected is not null &&
                   !string.IsNullOrWhiteSpace(selected.MerchantId) &&
                   !string.IsNullOrWhiteSpace(selected.MerchantKey)
                ? selected
                : null;
        }

        private string TransactionUrl(CieloCredentials credentials) =>
            credentials.Environment == CieloEnvironmentEnum.Production
                ? _options.ProductionTransactionUrl
                : _options.SandboxTransactionUrl;

        private string QueryUrl(CieloCredentials credentials) =>
            credentials.Environment == CieloEnvironmentEnum.Production
                ? _options.ProductionQueryUrl
                : _options.SandboxQueryUrl;

        private static string BuildUrl(string baseUrl, string relativeUrl) =>
            $"{baseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";

        private static string BuildAmountQuery(long? amount, long? serviceTaxAmount)
        {
            var parameters = new List<string>(2);

            if (amount.HasValue)
                parameters.Add($"amount={amount.Value}");

            if (serviceTaxAmount.HasValue)
                parameters.Add($"serviceTaxAmount={serviceTaxAmount.Value}");

            return parameters.Count == 0 ? string.Empty : $"?{string.Join("&", parameters)}";
        }

        private static string? ValidateSaleRequest(CieloSaleRequest request)
        {
            if (request is null)
                return "Informe os dados da cobranca.";

            if (string.IsNullOrWhiteSpace(request.MerchantOrderId))
                return "O MerchantOrderId e obrigatorio.";

            if (request.MerchantOrderId.Length > 50 || !request.MerchantOrderId.All(char.IsLetterOrDigit))
                return "O MerchantOrderId aceita apenas letras e numeros, com no maximo 50 caracteres.";

            var payment = request.Payment;
            if (payment is null || string.IsNullOrWhiteSpace(payment.Type))
                return "O meio de pagamento e obrigatorio.";

            if (payment.Amount <= 0)
                return "O valor da cobranca deve ser maior que zero.";

            return payment.Type switch
            {
                CieloPaymentTypes.CreditCard or CieloPaymentTypes.DebitCard => ValidateCard(payment),
                CieloPaymentTypes.Boleto => ValidateBoleto(request),
                _ => null
            };
        }

        private static string? ValidateCard(CieloPaymentRequest payment)
        {
            var card = payment.CreditCard;
            if (card is null)
                return "Os dados do cartao sao obrigatorios.";

            if (string.IsNullOrWhiteSpace(card.CardToken))
            {
                if (string.IsNullOrWhiteSpace(card.CardNumber))
                    return "O numero do cartao e obrigatorio.";

                if (string.IsNullOrWhiteSpace(card.Holder))
                    return "O nome impresso no cartao e obrigatorio.";

                if (string.IsNullOrWhiteSpace(card.ExpirationDate))
                    return "A validade do cartao e obrigatoria.";

                if (string.IsNullOrWhiteSpace(card.Brand))
                    return "A bandeira do cartao e obrigatoria.";
            }

            if (payment.Installments is < 1)
                return "O numero de parcelas deve ser maior que zero.";

            return null;
        }

        private static string? ValidateBoleto(CieloSaleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Payment.Provider))
                return "O banco emissor do boleto e obrigatorio.";

            if (string.IsNullOrWhiteSpace(request.Customer?.Name))
                return "O nome do sacado e obrigatorio para boleto.";

            if (string.IsNullOrWhiteSpace(request.Customer?.Identity))
                return "O CPF ou CNPJ do sacado e obrigatorio para boleto.";

            return null;
        }

        private const string MissingCredentialsMessage = "Credenciais da Cielo nao foram informadas.";
        private const string InvalidResponseMessage = "Resposta invalida retornada pela Cielo.";

        private sealed record CieloHttpResponse(bool IsSuccess, string Content, string? ErrorMessage);

        private sealed record RecurrentPaymentChange(string Resource, object Body);
    }
}
