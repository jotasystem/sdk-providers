namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Periodicidade da recorrencia agendada na Cielo. O valor numerico corresponde
    /// ao retornado pela API no no <c>RecurrentPayment.Interval</c>.
    /// </summary>
    public enum CieloRecurrenceIntervalEnum
    {
        Monthly = 1,
        Bimonthly = 2,
        Quarterly = 3,
        SemiAnnual = 6,
        Annual = 12
    }
}
