namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Chaves lidas do dicionario de metadados da cobranca. Aqui ficam apenas os ajustes
    /// especificos da Cielo: cartao, endereco e recorrencia chegam pelo proprio contrato
    /// de pagamento (<c>Card</c>, <c>Customer.Address</c> e <c>Recurrence</c>).
    /// </summary>
    public static class CieloMetadataKeys
    {
        /// <summary>Texto exibido na fatura do portador, ate 13 caracteres.</summary>
        public const string SoftDescriptor = "soft_descriptor";

        /// <summary>Captura automatica da autorizacao de credito.</summary>
        public const string Capture = "capture";

        /// <summary>Banco emissor do boleto: <c>Bradesco2</c>, <c>BancoDoBrasil3</c> ou <c>Simulado</c>.</summary>
        public const string BoletoProvider = "boleto_provider";

        public const string BoletoAssignor = "boleto_assignor";
        public const string BoletoInstructions = "boleto_instructions";
        public const string BoletoDemonstrative = "boleto_demonstrative";
        public const string BoletoIdentification = "boleto_identification";
        public const string BoletoNumber = "boleto_number";
    }
}
