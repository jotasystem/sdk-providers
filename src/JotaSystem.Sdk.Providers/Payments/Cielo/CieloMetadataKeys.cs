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

        /// <summary>Titulo exibido na pagina do link de pagamento.</summary>
        public const string LinkName = "link_name";

        /// <summary>Descricao exibida na pagina do link de pagamento.</summary>
        public const string LinkDescription = "link_description";

        /// <summary>Natureza do link: <c>Payment</c>, <c>Digital</c>, <c>Service</c>, <c>Asset</c> ou <c>Recurrent</c>.</summary>
        public const string LinkProductType = "link_product_type";

        /// <summary>Frete do link: <c>WithoutShipping</c>, <c>Free</c>, <c>FixedAmount</c>, <c>Correios</c> ou <c>WithoutShippingPickUp</c>.</summary>
        public const string LinkShippingType = "link_shipping_type";

        /// <summary>Meios de pagamento liberados na pagina do link, separados por virgula.</summary>
        public const string LinkPaymentTypes = "link_payment_types";

        /// <summary>Numero maximo de parcelas oferecido na pagina do link.</summary>
        public const string LinkMaxInstallments = "link_max_installments";

        /// <summary>Identificador do item no link de pagamento.</summary>
        public const string LinkSku = "link_sku";
    }
}
