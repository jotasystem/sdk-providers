using JotaSystem.Sdk.Providers.Logistics.Address.ViaCep;
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

            var provider = new ViaCepProvider(new ViaCep.ViaCepClient(httpClient));

            // Act
            var result = await provider.GetAddressByCepAsync("01001-000", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("01001-000", result!.Data?.ZipCode);
            Assert.Equal("Praça da Sé", result.Data?.Street);
        }

        [Fact]
        public async Task GetCepsByAddressAsync_Should_Return_Results_When_Address_Valid()
        {
            // Arrange
            var json = """
            [
                {
                    "cep": "01311-000",
                    "logradouro": "Avenida Paulista",
                    "bairro": "Bela Vista",
                    "localidade": "São Paulo",
                    "uf": "SP"
                },
                {
                    "cep": "01310-100",
                    "logradouro": "Avenida Paulista",
                    "bairro": "Bela Vista",
                    "localidade": "São Paulo",
                    "uf": "SP"
                }
            ]
            """;

            var handler = new MockHttpMessageHandler(json);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://viacep.com.br/")
            };
            var provider = new ViaCepProvider(new ViaCep.ViaCepClient(httpClient));

            // Act
            var result = (await provider.GetCepsByAddressAsync(
                " sp ",
                "  São   Paulo ",
                " Avenida   Paulista ",
                TestContext.Current.CancellationToken)).Data?.ToList();

            // Assert
            Assert.Equal(2, result?.Count);
            Assert.Equal("01311-000", result![0].ZipCode);
            Assert.Equal("Avenida Paulista", result[0].Street);
            Assert.Equal(
                "https://viacep.com.br/ws/SP/S%C3%A3o%20Paulo/Avenida%20Paulista/json",
                handler.LastRequestUri?.AbsoluteUri);
        }

        [Theory]
        [InlineData("", "São Paulo", "Paulista", "state")]
        [InlineData("S", "São Paulo", "Paulista", "state")]
        [InlineData("123", "São Paulo", "Paulista", "state")]
        [InlineData("SP", "SP", "Paulista", "city")]
        [InlineData("SP", "São Paulo", "Sé", "street")]
        public async Task GetCepsByAddressAsync_Should_Throw_When_Address_Invalid(
            string state,
            string city,
            string street,
            string expectedParameter)
        {
            // Arrange
            var provider = new ViaCepProvider(new ViaCep.ViaCepClient(new HttpClient()));

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => provider.GetCepsByAddressAsync(
                    state,
                    city,
                    street,
                    TestContext.Current.CancellationToken));

            // Assert
            Assert.Equal(expectedParameter, exception.ParamName);
        }
    }
}
