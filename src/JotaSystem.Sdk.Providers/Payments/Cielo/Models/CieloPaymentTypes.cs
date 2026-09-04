namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Valores aceitos pela Cielo no campo <c>Payment.Type</c>.
    /// </summary>
    public static class CieloPaymentTypes
    {
        public const string CreditCard = "CreditCard";
        public const string DebitCard = "DebitCard";
        public const string Boleto = "Boleto";
        public const string Pix = "Pix";
    }
}
