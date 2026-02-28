using JotaSystem.Sdk.Providers.Address.ViaCep;
using System.Net;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class ViaCepProviderTests
    {
        [Fact]
        public async Task GetAddressAsync_Should_Return_Success_When_Cep_Valid()
        {
            // Arrange
            var json = """
            {
                "cep": "01001-000",
                "logradouro": "Praça da Sé",
                "bairro": "Sé",
                "localidade": "São Paulo",
                "uf": "SP"
            }
            """;

            var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://viacep.com.br/")
            };

            var provider = new ViaCepProvider(httpClient);

            // Act
            var result = await provider.GetAddressByCepAsync("01001-000", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("01001-000", result!.ZipCode);
            Assert.Equal("Praça da Sé", result.Street);
        }
    }
}