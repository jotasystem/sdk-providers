using JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models;
using JotaSystem.Sdk.Providers.Payments.Cielo.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link
{
    internal sealed class CieloLinkProvider(
        IHttpClientFactory httpClientFactory,
        CieloOptions options,
        ICieloAuthTokenCache authTokenCache) : ICieloLinkProvider
    {
        private const string TokenPath = "api/public/v2/token";
        private const string ProductsPath = "api/public/v1/products";
        private const string OrdersPath = "api/public/v2/orders";
        private const int NameMaxLength = 128;
        private const int DescriptionMaxLength = 256;
        private const int OrderNumberMaxLength = 20;

        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly CieloOptions _options = options;
        private readonly ICieloAuthTokenCache _authTokenCache = authTokenCache;

        public async Task<ApiResponse<CieloLinkResponse>> CreateLinkAsync(
            CieloLinkRequest request,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            var validationError = ValidateLinkRequest(request);
            if (validationError is not null)
                return ApiResponse<CieloLinkResponse>.CreateFail(validationError);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloLinkResponse>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(
                HttpMethod.Post,
                BuildUrl($"{ProductsPath}/"),
                selectedCredentials,
                request,
                cancellationToken);

            return ReadLink(response);
        }

        public async Task<ApiResponse<CieloLinkResponse>> GetLinkAsync(
            string linkId,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(linkId))
                return ApiResponse<CieloLinkResponse>.CreateFail(MissingLinkMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloLinkResponse>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(
                HttpMethod.Get,
                BuildUrl($"{ProductsPath}/{Escape(linkId)}"),
                selectedCredentials,
                body: null,
                cancellationToken);

            return ReadLink(response);
        }

        public async Task<ApiResponse<bool>> DeleteLinkAsync(
            string linkId,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(linkId))
                return ApiResponse<bool>.CreateFail(MissingLinkMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<bool>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(
                HttpMethod.Delete,
                BuildUrl($"{ProductsPath}/{Escape(linkId)}"),
                selectedCredentials,
                body: null,
                cancellationToken);

            return response.IsSuccess
                ? ApiResponse<bool>.CreateSuccess(true)
                : ApiResponse<bool>.CreateFail(response.ErrorMessage!);
        }

        public async Task<ApiResponse<CieloLinkOrderList>> GetLinkOrdersAsync(
            string linkId,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(linkId))
                return ApiResponse<CieloLinkOrderList>.CreateFail(MissingLinkMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloLinkOrderList>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(
                HttpMethod.Get,
                BuildUrl($"{ProductsPath}/{Escape(linkId)}/payments"),
                selectedCredentials,
                body: null,
                cancellationToken);

            if (!response.IsSuccess)
                return ApiResponse<CieloLinkOrderList>.CreateFail(response.ErrorMessage!);

            var data = Deserialize<CieloLinkOrderList>(response.Content);
            if (data is null)
                return ApiResponse<CieloLinkOrderList>.CreateFail(InvalidResponseMessage);

            data.RawPayload = response.Content;
            return ApiResponse<CieloLinkOrderList>.CreateSuccess(data);
        }

        public async Task<ApiResponse<CieloLinkOrder>> GetOrderAsync(
            string checkoutOrderNumber,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(checkoutOrderNumber))
                return ApiResponse<CieloLinkOrder>.CreateFail(MissingOrderMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloLinkOrder>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(
                HttpMethod.Get,
                BuildUrl($"{OrdersPath}/{Escape(checkoutOrderNumber)}"),
                selectedCredentials,
                body: null,
                cancellationToken);

            if (!response.IsSuccess)
                return ApiResponse<CieloLinkOrder>.CreateFail(response.ErrorMessage!);

            var data = Deserialize<CieloLinkOrder>(response.Content);
            if (data is null)
                return ApiResponse<CieloLinkOrder>.CreateFail(InvalidResponseMessage);

            data.RawPayload = response.Content;
            return ApiResponse<CieloLinkOrder>.CreateSuccess(data);
        }

        public async Task<ApiResponse<CieloLinkOperationResponse>> VoidOrderAsync(
            string checkoutOrderNumber,
            long? amount = null,
            CieloLinkCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(checkoutOrderNumber))
                return ApiResponse<CieloLinkOperationResponse>.CreateFail(MissingOrderMessage);

            if (amount.HasValue && amount.Value <= 0)
                return ApiResponse<CieloLinkOperationResponse>.CreateFail(
                    "O valor do cancelamento deve ser maior que zero.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<CieloLinkOperationResponse>.CreateFail(MissingCredentialsMessage);

            var query = amount.HasValue ? $"?amount={amount.Value}" : string.Empty;
            var response = await SendAsync(
                HttpMethod.Put,
                BuildUrl($"{OrdersPath}/{Escape(checkoutOrderNumber)}/void{query}"),
                selectedCredentials,
                body: null,
                cancellationToken);

            if (!response.IsSuccess)
                return ApiResponse<CieloLinkOperationResponse>.CreateFail(response.ErrorMessage!);

            var data = Deserialize<CieloLinkOperationResponse>(response.Content)
                ?? new CieloLinkOperationResponse { Success = true };
            data.RawPayload = response.Content;

            return data.Success || data.Status is not null
                ? ApiResponse<CieloLinkOperationResponse>.CreateSuccess(data)
                : ApiResponse<CieloLinkOperationResponse>.CreateFail(
                    data.ReturnMessage ?? "A Cielo recusou o cancelamento do pedido.");
        }

        private async Task<ApiResponse<string>> GetAccessTokenAsync(
            CieloLinkCredentials credentials,
            CancellationToken cancellationToken)
        {
            var cacheKey = CreateCredentialKey(credentials);
            var minimumExpiration = DateTimeOffset.UtcNow.Add(_options.AuthTokenExpirationMargin);

            if (_authTokenCache.TryGet(cacheKey, minimumExpiration, out var cachedToken))
                return ApiResponse<string>.CreateSuccess(cachedToken);

            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, BuildUrl(TokenPath));
                var basicCredentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{credentials.ClientId}:{credentials.ClientSecret}"));
                message.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicCredentials);
                message.Content = new StringContent(
                    string.Empty,
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

                var client = _httpClientFactory.CreateClient(CieloHttpClientNames.Default);
                using var response = await client.SendAsync(message, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return ApiResponse<string>.CreateFail(
                        $"Erro ao autenticar no Link de Pagamento: {DescribeError(response.StatusCode, content)}");

                var token = Deserialize<CieloAuthToken>(content);
                if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
                    return ApiResponse<string>.CreateFail(InvalidResponseMessage);

                _authTokenCache.Set(cacheKey, token.AccessToken, DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn));
                return ApiResponse<string>.CreateSuccess(token.AccessToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResponse<string>.CreateFail(TimeoutMessage);
            }
            catch (HttpRequestException ex)
            {
                return ApiResponse<string>.CreateFail($"Falha na comunicacao com a Cielo: {ex.Message}");
            }
        }

        private async Task<CieloLinkHttpResponse> SendAsync(
            HttpMethod method,
            string url,
            CieloLinkCredentials credentials,
            object? body,
            CancellationToken cancellationToken)
        {
            var accessToken = await GetAccessTokenAsync(credentials, cancellationToken);
            if (!accessToken.Success)
                return new CieloLinkHttpResponse(false, string.Empty, accessToken.ErrorMessage);

            try
            {
                using var message = new HttpRequestMessage(method, url);
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Data);

                if (body is not null)
                    message.Content = new StringContent(
                        CieloLinkJson.Serialize(body),
                        Encoding.UTF8,
                        "application/json");

                var client = _httpClientFactory.CreateClient(CieloHttpClientNames.Default);
                using var response = await client.SendAsync(message, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                return response.IsSuccessStatusCode
                    ? new CieloLinkHttpResponse(true, content, null)
                    : new CieloLinkHttpResponse(false, content, DescribeError(response.StatusCode, content));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new CieloLinkHttpResponse(false, string.Empty, TimeoutMessage);
            }
            catch (HttpRequestException ex)
            {
                return new CieloLinkHttpResponse(false, string.Empty, $"Falha na comunicacao com a Cielo: {ex.Message}");
            }
        }

        private static ApiResponse<CieloLinkResponse> ReadLink(CieloLinkHttpResponse response)
        {
            if (!response.IsSuccess)
                return ApiResponse<CieloLinkResponse>.CreateFail(response.ErrorMessage!);

            var data = Deserialize<CieloLinkResponse>(response.Content);
            if (data is null || string.IsNullOrWhiteSpace(data.Id))
                return ApiResponse<CieloLinkResponse>.CreateFail(InvalidResponseMessage);

            data.RawPayload = response.Content;
            return ApiResponse<CieloLinkResponse>.CreateSuccess(data);
        }

        private static string? ValidateLinkRequest(CieloLinkRequest request)
        {
            if (request is null)
                return "Informe os dados do link de pagamento.";

            if (string.IsNullOrWhiteSpace(request.Type))
                return "O tipo do link de pagamento e obrigatorio.";

            if (string.IsNullOrWhiteSpace(request.Name))
                return "O nome exibido no link de pagamento e obrigatorio.";

            if (request.Name.Length > NameMaxLength)
                return $"O nome do link aceita no maximo {NameMaxLength} caracteres.";

            if (request.Description?.Length > DescriptionMaxLength)
                return $"A descricao do link aceita no maximo {DescriptionMaxLength} caracteres.";

            if (request.Price <= 0)
                return "O valor do link de pagamento deve ser maior que zero.";

            if (request.OrderNumber?.Length > OrderNumberMaxLength)
                return $"O numero do pedido aceita no maximo {OrderNumberMaxLength} caracteres.";

            if (request.MaxNumberOfInstallments is < 1 or > 18)
                return "O numero maximo de parcelas deve estar entre 1 e 18.";

            if (string.Equals(request.Shipping?.Type, CieloLinkShippingTypes.FixedAmount, StringComparison.OrdinalIgnoreCase) &&
                (request.Shipping!.Price is null or <= 0 || string.IsNullOrWhiteSpace(request.Shipping.Name)))
                return "Informe o nome e o valor do frete para o tipo FixedAmount.";

            if (string.Equals(request.Type, CieloLinkProductTypes.Recurrent, StringComparison.OrdinalIgnoreCase) &&
                request.Recurrent is null)
                return "Informe a periodicidade da recorrencia do link.";

            return null;
        }

        private CieloLinkCredentials? ResolveCredentials(CieloLinkCredentials? credentials)
        {
            var selected = credentials ?? _options.DefaultLinkCredentials;
            return selected is not null && selected.IsFilled ? selected : null;
        }

        private static string CreateCredentialKey(CieloLinkCredentials credentials)
        {
            var value = $"link\n{credentials.ClientId}\n{credentials.ClientSecret}";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        }

        private string BuildUrl(string relativeUrl) =>
            $"{_options.LinkApiUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";

        private static string Escape(string value) => Uri.EscapeDataString(value.Trim());

        private static T? Deserialize<T>(string content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                return CieloLinkJson.Deserialize<T>(content);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string DescribeError(HttpStatusCode statusCode, string content)
        {
            var messages = ReadErrorMessages(content);
            if (messages.Count > 0)
                return string.Join(" | ", messages);

            return string.IsNullOrWhiteSpace(content)
                ? $"A Cielo respondeu com o status {(int)statusCode}."
                : $"A Cielo respondeu com o status {(int)statusCode}: {content}";
        }

        // O Checkout Cielo devolve o erro ora como lista, ora como objeto com Message,
        // ora como dicionario de validacao por campo. Os tres caem aqui.
        private static List<string> ReadErrorMessages(string content)
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(content))
                return messages;

            try
            {
                using var document = JsonDocument.Parse(content);
                ReadErrorMessages(document.RootElement, messages);
            }
            catch (JsonException)
            {
                return messages;
            }

            return messages;
        }

        private static void ReadErrorMessages(JsonElement element, List<string> messages)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    var text = element.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        messages.Add(text.Trim());
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                        ReadErrorMessages(item, messages);
                    break;

                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.NameEquals("Message") || property.NameEquals("message") ||
                            property.NameEquals("Errors") || property.NameEquals("errors") ||
                            property.NameEquals("ModelState") || property.NameEquals("modelState"))
                            ReadErrorMessages(property.Value, messages);
                    }
                    break;
            }
        }

        private const string MissingCredentialsMessage =
            "Credenciais da API Link de Pagamento nao foram configuradas para a integracao.";
        private const string MissingLinkMessage = "Informe o identificador do link de pagamento.";
        private const string MissingOrderMessage = "Informe o numero do pedido gerado pelo Checkout Cielo.";
        private const string InvalidResponseMessage = "Resposta invalida retornada pela Cielo.";
        private const string TimeoutMessage = "Tempo limite excedido na comunicacao com a Cielo.";

        private sealed record CieloLinkHttpResponse(bool IsSuccess, string Content, string? ErrorMessage);
    }
}
