namespace JotaSystem.Sdk.Providers.Tests
{
    public class ApiResponseTest
    {
        [Fact]
        public void Should_Create_Success_Response()
        {
            var response = ApiResponse<string>.CreateSuccess("ok");
            Assert.True(response.Success);
            Assert.Equal("ok", response.Data);
        }

        [Fact]
        public void Should_Create_Fail_Response()
        {
            var response = ApiResponse<string>.CreateFail("erro");
            Assert.False(response.Success);
            Assert.Equal("erro", response.ErrorMessage);
        }
    }
}