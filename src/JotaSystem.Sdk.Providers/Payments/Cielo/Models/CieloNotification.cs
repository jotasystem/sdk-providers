namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Corpo do post de notificacao enviado pela Cielo.
    /// </summary>
    public class CieloNotification
    {
        public string? PaymentId { get; set; }

        /// <summary>Preenchido nos eventos de recorrencia (<c>ChangeType</c> 2 e 4).</summary>
        public string? RecurrentPaymentId { get; set; }

        /// <summary>Tipo do evento. Ver <see cref="CieloChangeTypeEnum"/>.</summary>
        public int ChangeType { get; set; }
    }
}
