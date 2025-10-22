using JotaSystem.Sdk.Providers.Common;
using JotaSystem.Sdk.Providers.Services.ViaCep.Interfaces;
using JotaSystem.Sdk.Providers.Services.ViaCep.Models;

namespace JotaSystem.Sdk.Providers.Services.ViaCep
{
    public class ViaCepProvider : ProviderBase, IViaCepProvider
    {
        private const string BaseUrl = "https://viacep.com.br/ws";

        public ViaCepProvider(HttpClient httpClient) : base(httpClient)
        {
        }

        public async Task<ApiResponse<ViaCepResponse>> GetAddressAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                return ApiResponse<ViaCepResponse>.CreateFail("CEP inválido.");

            // remove caracteres não numéricos
            cep = new string(cep.Where(char.IsDigit).ToArray());

            var url = $"{BaseUrl}/{cep}/json/";
            return await SendRequestAsync<ViaCepResponse>(HttpMethod.Get, url);
        }
    }
}