using JotaSystem.Sdk.Providers.Common;

namespace JotaSystem.Sdk.Providers.Tests.Common
{
    public class JsonHelperTest
    {
        private class Person
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
        }

        [Fact]
        public void Deserialize_ValidJson_ReturnsSuccess()
        {
            var json = "{\"name\":\"João\",\"age\":30}";
            var result = JsonHelper.Deserialize<Person>(json);

            Assert.True(result.Success);
            Assert.Equal("João", result.Data?.Name);
            Assert.Equal(30, result.Data?.Age);
        }

        [Fact]
        public void Deserialize_InvalidJson_ReturnsFail()
        {
            var invalidJson = "{name: 'Invalid Json'}";
            var result = JsonHelper.Deserialize<Person>(invalidJson);

            Assert.False(result.Success);
            Assert.Contains("Erro ao desserializar JSON", result.ErrorMessage);
        }

        [Fact]
        public void Deserialize_EmptyJson_ReturnsFail()
        {
            var result = JsonHelper.Deserialize<Person>("");

            Assert.False(result.Success);
            Assert.Equal("JSON vazio ou nulo.", result.ErrorMessage);
        }

        [Fact]
        public void Serialize_Object_ReturnsCamelCaseJson()
        {
            var person = new Person { Name = "Maria", Age = 25 };
            var json = JsonHelper.Serialize(person);

            Assert.Contains("\"name\"", json);
            Assert.Contains("\"age\"", json);
        }
    }
}