using JotaSystem.Sdk.Providers.Services.ViaCep;
using System.Net;

namespace JotaSystem.Sdk.Providers.Tests.Services
{
    public class ViaCepProviderTests
    {
        [Fact]
        public async Task GetAddressAsync_Should_Return_Success_When_Cep_Valid()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler("{\"cep\":\"01001-000\",\"logradouro\":\"Praça da Sé\"}");
            var httpClient = new HttpClient(mockHandler);
            var provider = new ViaCepProvider(httpClient);

            // Act
            var result = await provider.GetAddressAsync("01001-000");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("01001-000", result.Data?.Cep);
        }
    }
}