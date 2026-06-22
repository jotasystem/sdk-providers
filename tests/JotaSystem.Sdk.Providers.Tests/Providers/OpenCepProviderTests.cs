using JotaSystem.Sdk.Providers.Logistics.Address.OpenCep;
using System.Net;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class OpenCepProviderTests
    {
        [Fact]
        public async Task GetAddressByCepAsync_Should_Return_Success_When_Cep_Valid()
        {
            var json = """
            {
                "cep": "15050-305",
                "logradouro": "Rua Josina Teixeira de Carvalho",
                "complemento": "",
                "unidade": "",
                "bairro": "Vila Anchieta",
                "localidade": "São José do Rio Preto",
                "uf": "SP",
                "estado": "São Paulo",
                "regiao": "Sudeste",
                "ibge": "3549805"
            }
            """;

            var handler = new MockHttpMessageHandler(json);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://opencep.com/")
            };
            var provider = new OpenCepProvider(httpClient);

            var result = await provider.GetAddressByCepAsync("15050-305");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("15050-305", result.Data.Cep);
            Assert.Equal("Rua Josina Teixeira de Carvalho", result.Data.Logradouro);
            Assert.Equal("São José do Rio Preto", result.Data.Localidade);
            Assert.Equal("SP", result.Data.Uf);
            Assert.Equal("3549805", result.Data.Ibge);
            Assert.Equal("https://opencep.com/v1/15050305.json", handler.LastRequestUri?.AbsoluteUri);
        }

        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("1234567")]
        [InlineData("123456789")]
        public async Task GetAddressByCepAsync_Should_Fail_When_Cep_Invalid(string cep)
        {
            var provider = new OpenCepProvider(new HttpClient());

            var result = await provider.GetAddressByCepAsync(cep);

            Assert.False(result.Success);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetAddressByCepAsync_Should_Fail_When_Api_Returns_Error()
        {
            var handler = new MockHttpMessageHandler("", HttpStatusCode.NotFound);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://opencep.com/")
            };
            var provider = new OpenCepProvider(httpClient);

            var result = await provider.GetAddressByCepAsync("99999999");

            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Erro ao consultar CEP no OpenCEP.", result.ErrorMessage);
        }
    }
}
