using JotaSystem.Sdk.Common.Helpers;
using JotaSystem.Sdk.Providers.Logistics.Shipping.Correios.Models;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.Correios
{
    internal class CorreiosProvider(
        IHttpClientFactory httpClientFactory,
        CorreiosOptions options,
        ICorreiosTokenCache tokenCache) : ICorreiosProvider
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly CorreiosOptions _options = options;
        private readonly ICorreiosTokenCache _tokenCache = tokenCache;

        public async Task<ApiResponse<List<CorreiosShippingResponse>>> CalculateShippingAsync(
            CorreiosShippingRequest request,
            CorreiosCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            var validationError = ValidateRequest(request);
            if (validationError is not null)
                return ApiResponse<List<CorreiosShippingResponse>>.CreateFail(validationError);

            var selectedCredentials = credentials ?? _options.DefaultCredentials;
            if (!AreValidCredentials(selectedCredentials))
                return ApiResponse<List<CorreiosShippingResponse>>.CreateFail(
                    "Credenciais dos Correios não foram informadas.");

            var tokenResponse = await GetTokenAsync(selectedCredentials!, cancellationToken);
            if (!tokenResponse.Success || string.IsNullOrWhiteSpace(tokenResponse.Data))
                return ApiResponse<List<CorreiosShippingResponse>>.CreateFail(
                    tokenResponse.ErrorMessage ?? "Erro ao autenticar nos Correios.");

            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, "nacional");
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Data);
                message.Content = new StringContent(
                    JsonHelper.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var client = _httpClientFactory.CreateClient(CorreiosHttpClientNames.Price);
                using var response = await client.SendAsync(message, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return ApiResponse<List<CorreiosShippingResponse>>.CreateFail(
                        $"Erro ao consultar frete nos Correios: {content}");

                var data = JsonHelper.Deserialize<List<CorreiosShippingResponse>>(content);
                return ApiResponse<List<CorreiosShippingResponse>>.CreateSuccess(data ?? []);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<CorreiosShippingResponse>>.CreateFail(
                    $"Erro inesperado ao consultar frete nos Correios: {ex.Message}");
            }
        }

        private async Task<ApiResponse<string>> GetTokenAsync(
            CorreiosCredentials credentials,
            CancellationToken cancellationToken)
        {
            var cacheKey = CreateCredentialKey(credentials);
            var minimumExpiration = DateTimeOffset.UtcNow.Add(_options.TokenExpirationMargin);

            if (_tokenCache.TryGet(cacheKey, minimumExpiration, out var cachedToken))
                return ApiResponse<string>.CreateSuccess(cachedToken);

            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, "autentica/cartaopostagem");
                var basicCredentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{credentials.UserName}:{credentials.AccessCode}"));
                message.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicCredentials);
                message.Content = new StringContent(
                    JsonHelper.Serialize(new { numero = credentials.PostingCardNumber }),
                    Encoding.UTF8,
                    "application/json");

                var client = _httpClientFactory.CreateClient(CorreiosHttpClientNames.Token);
                using var response = await client.SendAsync(message, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return ApiResponse<string>.CreateFail(
                        $"Erro ao autenticar nos Correios: {content}");

                var token = JsonHelper.Deserialize<CorreiosTokenResponse>(content);
                if (token is null || string.IsNullOrWhiteSpace(token.Token))
                    return ApiResponse<string>.CreateFail("Token inválido retornado pelos Correios.");

                _tokenCache.Set(cacheKey, token.Token, token.ExpiresAt);
                return ApiResponse<string>.CreateSuccess(token.Token);
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.CreateFail(
                    $"Erro inesperado ao autenticar nos Correios: {ex.Message}");
            }
        }

        private static string? ValidateRequest(CorreiosShippingRequest request)
        {
            if (request is null || request.Products.Count == 0)
                return "Informe ao menos um produto para cotação.";

            foreach (var product in request.Products)
            {
                if (string.IsNullOrWhiteSpace(product.ProductCode))
                    return "O código do produto dos Correios é obrigatório.";

                if (SanitizeZipCode(product.OriginZipCode).Length != 8)
                    return "CEP de origem deve conter 8 dígitos.";

                if (SanitizeZipCode(product.DestinationZipCode).Length != 8)
                    return "CEP de destino deve conter 8 dígitos.";

                product.OriginZipCode = SanitizeZipCode(product.OriginZipCode);
                product.DestinationZipCode = SanitizeZipCode(product.DestinationZipCode);
            }

            return null;
        }

        private static bool AreValidCredentials(CorreiosCredentials? credentials) =>
            credentials is not null &&
            !string.IsNullOrWhiteSpace(credentials.UserName) &&
            !string.IsNullOrWhiteSpace(credentials.AccessCode) &&
            !string.IsNullOrWhiteSpace(credentials.PostingCardNumber);

        private static string SanitizeZipCode(string value) =>
            new(value.Where(char.IsDigit).ToArray());

        private static string CreateCredentialKey(CorreiosCredentials credentials)
        {
            var value = $"{credentials.UserName}\n{credentials.AccessCode}\n{credentials.PostingCardNumber}";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        }
    }
}
