namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Alteracoes a aplicar em uma recorrencia agendada. Somente os campos informados
    /// sao enviados a Cielo, cada um em sua propria chamada.
    /// </summary>
    public sealed record CieloRecurrentPaymentUpdate
    {
        /// <summary>Novo valor da recorrencia, em centavos.</summary>
        public long? Amount { get; init; }

        /// <summary>Nova data de encerramento da recorrencia.</summary>
        public DateOnly? EndDate { get; init; }

        /// <summary>Nova data da proxima cobranca.</summary>
        public DateOnly? NextPaymentDate { get; init; }

        /// <summary>Novo dia do mes em que a cobranca ocorre.</summary>
        public int? RecurrencyDay { get; init; }

        /// <summary>Nova periodicidade da recorrencia.</summary>
        public CieloRecurrenceIntervalEnum? Interval { get; init; }

        /// <summary>Novos dados do comprador.</summary>
        public CieloCustomer? Customer { get; init; }

        /// <summary>Novos dados de pagamento, usados para trocar o cartao da recorrencia.</summary>
        public CieloPaymentRequest? Payment { get; init; }

        /// <summary>Indica se ha alguma alteracao a aplicar.</summary>
        public bool HasChanges =>
            Amount.HasValue ||
            EndDate.HasValue ||
            NextPaymentDate.HasValue ||
            RecurrencyDay.HasValue ||
            Interval.HasValue ||
            Customer is not null ||
            Payment is not null;
    }
}
