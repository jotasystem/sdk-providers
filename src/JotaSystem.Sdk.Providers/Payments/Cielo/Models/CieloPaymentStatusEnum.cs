namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Status da transacao retornado pela Cielo no no <c>Payment.Status</c>.
    /// </summary>
    public enum CieloPaymentStatusEnum
    {
        /// <summary>Aguardando atualizacao de status.</summary>
        NotFinished = 0,

        /// <summary>Apto a ser capturado (credito) ou registrado (boleto e Pix).</summary>
        Authorized = 1,

        /// <summary>Pagamento confirmado e finalizado.</summary>
        PaymentConfirmed = 2,

        /// <summary>Pagamento negado pelo autorizador.</summary>
        Denied = 3,

        /// <summary>Pagamento cancelado no mesmo dia da autorizacao.</summary>
        Voided = 10,

        /// <summary>Pagamento cancelado apos as 23h59 do dia da autorizacao.</summary>
        Refunded = 11,

        /// <summary>Aguardando retorno da instituicao financeira.</summary>
        Pending = 12,

        /// <summary>Pagamento cancelado por falha no processamento.</summary>
        Aborted = 13,

        /// <summary>Recorrencia agendada.</summary>
        Scheduled = 20
    }
}
