namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models
{
    /// <summary>Valores aceitos no campo <c>type</c> do link.</summary>
    public static class CieloLinkProductTypes
    {
        /// <summary>Bem fisico, com frete.</summary>
        public const string Asset = "Asset";

        /// <summary>Produto digital, sem frete.</summary>
        public const string Digital = "Digital";

        /// <summary>Servico.</summary>
        public const string Service = "Service";

        /// <summary>Cobranca avulsa, sem catalogo nem frete.</summary>
        public const string Payment = "Payment";

        /// <summary>Assinatura com cobranca repetida pela Cielo.</summary>
        public const string Recurrent = "Recurrent";
    }

    /// <summary>Valores aceitos no campo <c>shipping.type</c> do link.</summary>
    public static class CieloLinkShippingTypes
    {
        public const string Correios = "Correios";
        public const string FixedAmount = "FixedAmount";
        public const string Free = "Free";
        public const string WithoutShippingPickUp = "WithoutShippingPickUp";
        public const string WithoutShipping = "WithoutShipping";
    }

    /// <summary>Meios de pagamento que podem ser liberados na pagina do link.</summary>
    public static class CieloLinkPaymentTypes
    {
        public const string CreditCard = "CreditCard";
        public const string DebitCard = "DebitCard";
        public const string OnlineDebit = "OnlineDebit";
        public const string Boleto = "Boleto";
        public const string Pix = "Pix";
        public const string OpenFinance = "OpenFinance";
        public const string QrCode = "QrCode";
        public const string QrCodeDebit = "QrCodeDebit";
        public const string MultiBenefits = "MultiBenefits";
    }
}
