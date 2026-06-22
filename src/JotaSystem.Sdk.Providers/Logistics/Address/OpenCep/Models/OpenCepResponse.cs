namespace JotaSystem.Sdk.Providers.Logistics.Address.OpenCep.Models
{
    public class OpenCepResponse
    {
        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
        public string Unidade { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Localidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Regiao { get; set; } = string.Empty;
        public string Ibge { get; set; } = string.Empty;
    }
}
