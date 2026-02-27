using JotaSystem.Sdk.Providers.Address.ViaCep;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class ViaCepProviderTests
    {
        [Fact]
        public async Task GetAddressAsync_Should_Return_Success_When_Cep_Valid()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler("{\"cep\":\"01001-000\",\"logradouro\":\"Praça da Sé\"}");
            var provider = new ViaCepProvider();

            // Act
            var result = await provider.GetAddressByCepAsync("01001-000", CancellationToken.None);

            // Assert
            Assert.Equal("01001-000", result!.ZipCode);
        }
    }
}