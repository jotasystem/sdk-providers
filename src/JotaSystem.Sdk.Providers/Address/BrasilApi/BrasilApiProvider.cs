using JotaSystem.Sdk.Providers.Abstractions;
using JotaSystem.Sdk.Providers.Address.BrasilApi.Models;

namespace JotaSystem.Sdk.Providers.Address.BrasilApi
{
    public class BrasilApiProvider(HttpClient httpClient) : ProviderBase(httpClient), IBrasilApiProvider
    {
        private const string BaseUrl = "https://brasilapi.com.br/api";

        public async Task<ApiResponse<List<BrasilApiBankResponse>>> GetBanksAsync()
        {
            var response = await SendRequestAsync<List<BrasilApiBankResponse>>(HttpMethod.Get, $"{BaseUrl}/banks/v1");

            if (!response.Success)
                return ApiResponse<List<BrasilApiBankResponse>>.CreateFail("Erro ao consultar lista de bancos na BrasilAPI.");

            return response;
        }

        public async Task<ApiResponse<BrasilApiCepResponse>> GetCepAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                return ApiResponse<BrasilApiCepResponse>.CreateFail("CEP inválido.");

            cep = new string(cep.Where(char.IsDigit).ToArray());

            if (cep.Length != 8)
                return ApiResponse<BrasilApiCepResponse>.CreateFail("CEP deve conter 8 dígitos.");

            var url = $"{BaseUrl}/cep/v2/{cep}";

            var response = await SendRequestAsync<BrasilApiCepResponse>(HttpMethod.Get, url);

            if (!response.Success)
                return ApiResponse<BrasilApiCepResponse>.CreateFail("Erro ao consultar CEP na BrasilAPI.");

            return response;
        }
    }
}