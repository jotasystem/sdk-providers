namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models
{
    /// <summary>
    /// Corpo da criacao de um link de pagamento (<c>POST /api/public/v1/products/</c>).
    /// </summary>
    public class CieloLinkRequest
    {
        /// <summary>Numero do pedido na loja, usado na conciliacao. Ate 20 caracteres alfanumericos.</summary>
        public string? OrderNumber { get; set; }

        /// <summary>Natureza do que esta sendo cobrado. Ver <see cref="CieloLinkProductTypes"/>.</summary>
        public string Type { get; set; } = CieloLinkProductTypes.Payment;

        /// <summary>Titulo exibido na pagina de pagamento. Ate 128 caracteres.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Descricao exibida na pagina de pagamento. Ate 256 caracteres.</summary>
        public string? Description { get; set; }

        /// <summary>Exibe a descricao no checkout.</summary>
        public bool? ShowDescription { get; set; }

        /// <summary>Valor em centavos.</summary>
        public long Price { get; set; }

        /// <summary>Vencimento do link no formato <c>yyyy-MM-dd</c> ou <c>yyyy-MM-dd HH:mm:ss</c>.</summary>
        public string? ExpirationDate { get; set; }

        /// <summary>Texto exibido na fatura do portador. Ate 13 caracteres.</summary>
        public string? SoftDescriptor { get; set; }

        /// <summary>Numero maximo de parcelas que o pagador pode escolher, de 1 a 18.</summary>
        public int? MaxNumberOfInstallments { get; set; }

        /// <summary>Quantidade de pagamentos aprovados que o link aceita antes de encerrar.</summary>
        public int? Quantity { get; set; }

        /// <summary>Identificador do item na loja. Ate 32 caracteres.</summary>
        public string? Sku { get; set; }

        public CieloLinkShipping? Shipping { get; set; }
        public CieloLinkRecurrent? Recurrent { get; set; }

        /// <summary>Restringe os meios de pagamento e as carteiras exibidos na pagina.</summary>
        public CieloLinkCustomConfiguration? CustomLinkConfiguration { get; set; }
    }

    /// <summary>Frete do link. Ver <see cref="CieloLinkShippingTypes"/>.</summary>
    public class CieloLinkShipping
    {
        public string Type { get; set; } = CieloLinkShippingTypes.WithoutShipping;

        /// <summary>Nome do frete, obrigatorio em <c>FixedAmount</c>.</summary>
        public string? Name { get; set; }

        /// <summary>Valor do frete em centavos, obrigatorio em <c>FixedAmount</c>.</summary>
        public long? Price { get; set; }
    }

    /// <summary>Recorrencia do link, valida apenas no tipo <c>Recurrent</c>.</summary>
    public class CieloLinkRecurrent
    {
        /// <summary><c>Monthly</c>, <c>Bimonthly</c>, <c>Quarterly</c>, <c>SemiAnnual</c> ou <c>Annual</c>.</summary>
        public string Interval { get; set; } = "Monthly";

        /// <summary>Encerramento da recorrencia no formato <c>yyyy-MM-dd</c>. Sem data, a assinatura nao tem fim.</summary>
        public string? EndDate { get; set; }
    }

    /// <summary>Meios de pagamento e carteiras liberados na pagina do link.</summary>
    public class CieloLinkCustomConfiguration
    {
        /// <summary>Ver <see cref="CieloLinkPaymentTypes"/>. Vazio mantem a configuracao da loja.</summary>
        public List<string> PaymentTypes { get; set; } = [];

        /// <summary><c>GooglePay</c>, <c>ApplePay</c> ou <c>ClickToPay</c>.</summary>
        public List<string> Wallets { get; set; } = [];
    }
}
