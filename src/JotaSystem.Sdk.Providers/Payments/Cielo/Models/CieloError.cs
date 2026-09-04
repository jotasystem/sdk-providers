namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Erro de negocio devolvido pela Cielo em respostas com status 400.
    /// </summary>
    public class CieloError
    {
        public int Code { get; set; }
        public string? Message { get; set; }
    }
}
