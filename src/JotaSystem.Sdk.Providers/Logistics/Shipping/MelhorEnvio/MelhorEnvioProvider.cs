using JotaSystem.Sdk.Common.Helpers;
using JotaSystem.Sdk.Providers.Logistics.Shipping.MelhorEnvio.Models;
using System.Net.Http.Headers;
using System.Text;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.MelhorEnvio
{
    internal class MelhorEnvioProvider(HttpClient httpClient, MelhorEnvioOptions options) : IMelhorEnvioProvider
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly MelhorEnvioOptions _options = options;

        public async Task<ApiResponse<List<MelhorEnvioShippingQuoteResponse>>> CalculateShippingAsync(
            MelhorEnvioShippingQuoteRequest request,
            string? accessToken = null,
            CancellationToken cancellationToken = default)
        {
            var validationError = ValidateRequest(request);
            if (validationError is not null)
                return ApiResponse<List<MelhorEnvioShippingQuoteResponse>>.CreateFail(validationError);

            var selectedToken = accessToken ?? _options.AccessToken;
            if (string.IsNullOrWhiteSpace(selectedToken))
                return ApiResponse<List<MelhorEnvioShippingQuoteResponse>>.CreateFail(
                    "Token de acesso da Melhor Envio nao foi informado.");

            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, "api/v2/me/shipment/calculate");
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", selectedToken);
                message.Headers.UserAgent.ParseAdd(_options.UserAgent);
                message.Content = new StringContent(
                    JsonHelper.Serialize(CreatePayload(request)),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(message, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return ApiResponse<List<MelhorEnvioShippingQuoteResponse>>.CreateFail(
                        $"Erro ao consultar frete na Melhor Envio: {content}");

                var data = JsonHelper.Deserialize<List<MelhorEnvioShippingQuoteResponse>>(content);
                return ApiResponse<List<MelhorEnvioShippingQuoteResponse>>.CreateSuccess(data ?? []);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<MelhorEnvioShippingQuoteResponse>>.CreateFail(
                    $"Erro inesperado ao consultar frete na Melhor Envio: {ex.Message}");
            }
        }

        private static string? ValidateRequest(MelhorEnvioShippingQuoteRequest request)
        {
            if (request is null)
                return "Informe os dados para cotacao de frete.";

            if (SanitizeZipCode(request.From.PostalCode).Length != 8)
                return "CEP de origem deve conter 8 digitos.";

            if (SanitizeZipCode(request.To.PostalCode).Length != 8)
                return "CEP de destino deve conter 8 digitos.";

            request.From.PostalCode = SanitizeZipCode(request.From.PostalCode);
            request.To.PostalCode = SanitizeZipCode(request.To.PostalCode);

            if (request.Products.Count == 0 && request.Volumes.Count == 0)
                return "Informe ao menos um produto ou volume para cotacao.";

            foreach (var product in request.Products)
            {
                if (string.IsNullOrWhiteSpace(product.Id))
                    return "O identificador do produto e obrigatorio.";

                if (product.Width <= 0 || product.Height <= 0 || product.Length <= 0)
                    return "As dimensoes do produto devem ser maiores que zero.";

                if (product.Weight <= 0)
                    return "O peso do produto deve ser maior que zero.";

                if (product.InsuranceValue < 0)
                    return "O valor segurado do produto nao pode ser negativo.";

                if (product.Quantity <= 0)
                    return "A quantidade do produto deve ser maior que zero.";
            }

            foreach (var volume in request.Volumes)
            {
                if (volume.Width <= 0 || volume.Height <= 0 || volume.Length <= 0)
                    return "As dimensoes do volume devem ser maiores que zero.";

                if (volume.Weight <= 0)
                    return "O peso do volume deve ser maior que zero.";

                if (volume.Insurance < 0)
                    return "O valor segurado do volume nao pode ser negativo.";
            }

            return null;
        }

        private static string SanitizeZipCode(string value) =>
            new((value ?? string.Empty).Where(char.IsDigit).ToArray());

        private static Dictionary<string, object?> CreatePayload(MelhorEnvioShippingQuoteRequest request)
        {
            var payload = new Dictionary<string, object?>
            {
                ["from"] = request.From,
                ["to"] = request.To
            };

            if (request.Products.Count > 0)
                payload["products"] = request.Products;

            if (request.Volumes.Count > 0)
                payload["volumes"] = request.Volumes;

            if (request.Options is not null)
                payload["options"] = request.Options;

            if (!string.IsNullOrWhiteSpace(request.Services))
                payload["services"] = request.Services;

            return payload;
        }
    }
}
