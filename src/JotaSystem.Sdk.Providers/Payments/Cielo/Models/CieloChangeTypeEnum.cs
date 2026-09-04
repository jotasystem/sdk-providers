namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Tipo de evento enviado pela Cielo no post de notificacao (<c>ChangeType</c>).
    /// </summary>
    public enum CieloChangeTypeEnum
    {
        TransactionStatusChanged = 1,
        RecurrentOrderCreated = 2,
        AntifraudStatusChanged = 3,
        RecurrentPaymentStatusChanged = 4,
        CancellationDenied = 5,
        UnderpaidBoleto = 6,
        Chargeback = 7,
        FraudAlert = 8,
        PartialCancellation = 25
    }
}
