namespace JotaSystem.Sdk.Providers.Address.BrasilApi.Models
{
    public class BrasilApiCepResponse
    {
        public string Cep { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Neighborhood { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public Location? Location { get; set; }
    }

    public class Location
    {
        public string Type { get; set; } = string.Empty;
        public Coordinates? Ccoordinates { get; set; }
    }

    public class Coordinates
    {
        public string Longitude { get; set; } = string.Empty;
        public string Latitude { get; set; } = string.Empty;
    }
}