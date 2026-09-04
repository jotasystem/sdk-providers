namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Meio de pagamento da Cielo derivado do codigo configurado no sistema consumidor.
    /// </summary>
    public enum CieloPaymentMethodEnum
    {
        CreditCard = 0,
        RecurrentCreditCard = 1,
        Pix = 2,
        Boleto = 3
    }

    /// <summary>
    /// Codigos aceitos no campo de metodo do gateway. O codigo canonico e o primeiro
    /// de cada grupo; os demais sao sinonimos aceitos para facilitar a configuracao.
    /// </summary>
    public static class CieloMethodCodes
    {
        public const string CreditCard = "credit_card";
        public const string RecurrentCreditCard = "credit_card_recurrent";
        public const string Pix = "pix";
        public const string Boleto = "boleto";

        private static readonly Dictionary<string, CieloPaymentMethodEnum> _methods =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [CreditCard] = CieloPaymentMethodEnum.CreditCard,
                ["creditcard"] = CieloPaymentMethodEnum.CreditCard,
                ["cartao_credito"] = CieloPaymentMethodEnum.CreditCard,
                ["credito"] = CieloPaymentMethodEnum.CreditCard,

                [RecurrentCreditCard] = CieloPaymentMethodEnum.RecurrentCreditCard,
                ["credit_card_recurring"] = CieloPaymentMethodEnum.RecurrentCreditCard,
                ["cartao_credito_recorrente"] = CieloPaymentMethodEnum.RecurrentCreditCard,
                ["recorrencia"] = CieloPaymentMethodEnum.RecurrentCreditCard,
                ["assinatura"] = CieloPaymentMethodEnum.RecurrentCreditCard,

                [Pix] = CieloPaymentMethodEnum.Pix,

                [Boleto] = CieloPaymentMethodEnum.Boleto,
                ["boleto_bancario"] = CieloPaymentMethodEnum.Boleto,
                ["bank_slip"] = CieloPaymentMethodEnum.Boleto
            };

        /// <summary>
        /// Converte o codigo configurado na forma de pagamento para o meio de pagamento da Cielo.
        /// </summary>
        public static bool TryResolve(string? methodCode, out CieloPaymentMethodEnum method)
        {
            method = CieloPaymentMethodEnum.CreditCard;

            if (string.IsNullOrWhiteSpace(methodCode))
                return false;

            return _methods.TryGetValue(methodCode.Trim().Replace("-", "_"), out method);
        }
    }
}
