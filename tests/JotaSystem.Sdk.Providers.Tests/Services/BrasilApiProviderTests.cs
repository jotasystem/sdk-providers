using JotaSystem.Sdk.Providers.Address.BrasilApi;
using System.Net;

namespace JotaSystem.Sdk.Providers.Tests.Services
{
    public class BrasilApiProviderTests
    {
        [Fact]
        public async Task GetBanksAsync_Should_Return_Success_When_Api_Returns_Data()
        {
            // Arrange
            var jsonResponse = """
            [
                {
                    "ispb": "00000000",
                    "name": "BANCO TESTE",
                    "code": 999,
                    "fullName": "BANCO TESTE S.A."
                }
            ]
            """;

            var mockHandler = new MockHttpMessageHandler(jsonResponse);
            var httpClient = new HttpClient(mockHandler);
            var provider = new BrasilApiProvider(httpClient);

            // Act
            var result = await provider.GetBanksAsync();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            Assert.Equal("BANCO TESTE", result.Data[0].Name);
            Assert.Equal(999, result.Data[0].Code);
        }

        [Fact]
        public async Task GetBanksAsync_Should_Fail_When_Api_Returns_Error()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler("", HttpStatusCode.BadRequest);
            var httpClient = new HttpClient(mockHandler);
            var provider = new BrasilApiProvider(httpClient);

            // Act
            var result = await provider.GetBanksAsync();

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetCepAsync_Should_Return_Success_When_Cep_Valid()
        {
            // Arrange
            var jsonResponse = """
            {
                "cep": "01001000",
                "state": "SP",
                "city": "São Paulo",
                "neighborhood": "Sé",
                "street": "Praça da Sé",
                "service": "brasilapi"
            }
            """;

            var mockHandler = new MockHttpMessageHandler(jsonResponse);
            var httpClient = new HttpClient(mockHandler);
            var provider = new BrasilApiProvider(httpClient);

            // Act
            var result = await provider.GetCepAsync("01001-000");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("01001000", result.Data?.Cep);
            Assert.Equal("Praça da Sé", result.Data?.Street);
            Assert.Equal("São Paulo", result.Data?.City);
            Assert.Equal("SP", result.Data?.State);
            Assert.Equal("Sé", result.Data?.Neighborhood);
        }

        [Fact]
        public async Task GetCepAsync_Should_Fail_When_Cep_Invalid()
        {
            // Arrange
            var provider = new BrasilApiProvider(new HttpClient());

            // Act
            var result = await provider.GetCepAsync("abc");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("CEP deve conter 8 dígitos.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetCepAsync_Should_Fail_When_Api_Returns_Error()
        {
            // Arrange – BrasilAPI retorna erro com status 400/404
            var mockHandler = new MockHttpMessageHandler("", HttpStatusCode.NotFound);
            var httpClient = new HttpClient(mockHandler);
            var provider = new BrasilApiProvider(httpClient);

            // Act
            var result = await provider.GetCepAsync("01001000");

            // Assert
            Assert.False(result.Success);
        }
    }
}
