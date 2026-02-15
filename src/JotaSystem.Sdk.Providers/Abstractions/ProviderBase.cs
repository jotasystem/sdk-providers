using JotaSystem.Sdk.Common.Helpers;
using System.Text;

namespace JotaSystem.Sdk.Providers.Abstractions
{
    public abstract class ProviderBase(HttpClient httpClient)
    {
        protected readonly HttpClient _httpClient = httpClient;

        protected async Task<ApiResponse<T>> SendRequestAsync<T>(HttpMethod method,
                                                                 string url,
                                                                 object? body = null,
                                                                 Dictionary<string, string>? headers = null,
                                                                 Dictionary<string, string>? queryParams = null,
                                                                 string contentType = "application/json",
                                                                 TimeSpan? timeout = null)
        {
            try
            {
                // Timeout customizado
                var client = _httpClient;
                if (timeout.HasValue)
                    client = new HttpClient { Timeout = timeout.Value };

                // Query params
                if (queryParams != null && queryParams.Count > 0)
                {
                    var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
                    url = url.Contains("?") ? $"{url}&{queryString}" : $"{url}?{queryString}";
                }

                using var request = new HttpRequestMessage(method, url);

                // Headers
                if (headers != null)
                {
                    foreach (var kv in headers)
                        request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }

                // Body
                if (body != null)
                {
                    if (contentType == "application/json")
                    {
                        var json = JsonHelper.Serialize(body);
                        request.Content = new StringContent(json, Encoding.UTF8, contentType);
                    }
                    else if (contentType == "application/x-www-form-urlencoded" && body is Dictionary<string, string> form)
                    {
                        request.Content = new FormUrlEncodedContent(form);
                    }
                    // futuramente multipart/form-data etc.
                }

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return ApiResponse<T>.CreateFail($"Erro {response.StatusCode}: {content}");

                return ApiResponse<T>.CreateSuccess(JsonHelper.Deserialize<T>(content)!);
            }
            catch (Exception ex)
            {
                return ApiResponse<T>.CreateFail($"Erro inesperado: {ex.Message}");
            }
        }
    }
}