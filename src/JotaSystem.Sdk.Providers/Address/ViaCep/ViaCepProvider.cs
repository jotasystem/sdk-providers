using JotaSystem.Sdk.Providers.Address.ViaCep.Models;
using JotaSystem.Sdk.Providers.Common;

namespace JotaSystem.Sdk.Providers.Address.ViaCep
{
    public class ViaCepProvider(HttpClient httpClient) : ProviderBase(httpClient), IViaCepProvider
    {
        private const string BaseUrl = "https://viacep.com.br/ws";

        public async Task<ApiResponse<ViaCepResponse>> GetAddressAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                return ApiResponse<ViaCepResponse>.CreateFail("CEP inválido.");

            // remove caracteres não numéricos
            cep = new string(cep.Where(char.IsDigit).ToArray());

            if (cep.Length != 8)
                return ApiResponse<ViaCepResponse>.CreateFail("CEP deve conter 8 dígitos.");

            var url = $"{BaseUrl}/{cep}/json/";

            var response = await SendRequestAsync<ViaCepResponse>(HttpMethod.Get, url);

            if (response.Success &&
                response.Data is not null &&
                response.Data.Erro == true)
            {
                return ApiResponse<ViaCepResponse>.CreateFail("CEP não encontrado.");
            }

            return response;
        }
    }
}