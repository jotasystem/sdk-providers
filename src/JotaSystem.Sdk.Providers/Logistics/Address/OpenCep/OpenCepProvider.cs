using JotaSystem.Sdk.Providers.Logistics.Address.OpenCep.Models;

namespace JotaSystem.Sdk.Providers.Logistics.Address.OpenCep
{
    public class OpenCepProvider(HttpClient httpClient) : ProviderBase(httpClient), IOpenCepProvider
    {
        public async Task<ApiResponse<OpenCepResponse>> GetAddressByCepAsync(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                return ApiResponse<OpenCepResponse>.CreateFail("CEP inválido.");

            var sanitizedCep = new string(cep.Where(char.IsDigit).ToArray());

            if (sanitizedCep.Length != 8)
                return ApiResponse<OpenCepResponse>.CreateFail("CEP deve conter 8 dígitos.");

            var response = await SendRequestAsync<OpenCepResponse>(
                HttpMethod.Get,
                $"v1/{sanitizedCep}.json");

            if (!response.Success)
                return ApiResponse<OpenCepResponse>.CreateFail("Erro ao consultar CEP no OpenCEP.");

            return response;
        }
    }
}
