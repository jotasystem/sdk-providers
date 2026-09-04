namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Requisicao de criacao de venda (<c>POST /1/sales/</c>). O no <c>Payment.Type</c>
    /// determina o meio de pagamento: credito, credito recorrente, Pix ou boleto.
    /// </summary>
    public class CieloSaleRequest
    {
        /// <summary>Numero do pedido na loja. Apenas letras e numeros, ate 50 caracteres.</summary>
        public string MerchantOrderId { get; set; } = string.Empty;

        public CieloCustomer? Customer { get; set; }

        public CieloPaymentRequest Payment { get; set; } = new();
    }

    /// <summary>
    /// No <c>Payment</c> da requisicao de venda.
    /// </summary>
    public class CieloPaymentRequest
    {
        /// <summary>Meio de pagamento. Ver <see cref="CieloPaymentTypes"/>.</summary>
        public string Type { get; set; } = CieloPaymentTypes.CreditCard;

        /// <summary>Valor da transacao em centavos.</summary>
        public long Amount { get; set; }

        public string? Currency { get; set; } = "BRL";
        public string? Country { get; set; } = "BRA";

        /// <summary>Numero de parcelas. Aplicavel a cartao de credito.</summary>
        public int? Installments { get; set; }

        /// <summary><c>ByMerchant</c> (loja) ou <c>ByIssuer</c> (emissor).</summary>
        public string? Interest { get; set; }

        /// <summary>Captura automatica apos a autorizacao.</summary>
        public bool? Capture { get; set; }

        public bool? Authenticate { get; set; }

        /// <summary>Marca a transacao como recorrencia propria da loja.</summary>
        public bool? Recurrent { get; set; }

        /// <summary>Texto exibido na fatura do portador, ate 13 caracteres.</summary>
        public string? SoftDescriptor { get; set; }

        /// <summary>Banco emissor do boleto ou meio de captura configurado na Cielo.</summary>
        public string? Provider { get; set; }

        public CieloCreditCardRequest? CreditCard { get; set; }

        /// <summary>Preenchido apenas na recorrencia agendada pela Cielo.</summary>
        public CieloRecurrentPaymentRequest? RecurrentPayment { get; set; }

        /// <summary>Vencimento do boleto no formato <c>yyyy-MM-dd</c>.</summary>
        public string? ExpirationDate { get; set; }

        public string? Instructions { get; set; }
        public string? Demonstrative { get; set; }
        public string? Assignor { get; set; }
        public string? Identification { get; set; }
        public string? BoletoNumber { get; set; }
    }

    /// <summary>
    /// Dados do cartao de credito enviados na autorizacao.
    /// </summary>
    public class CieloCreditCardRequest
    {
        public string? CardNumber { get; set; }
        public string? Holder { get; set; }

        /// <summary>Validade no formato <c>MM/yyyy</c>.</summary>
        public string? ExpirationDate { get; set; }

        public string? SecurityCode { get; set; }

        /// <summary><c>Visa</c>, <c>Master</c>, <c>Amex</c>, <c>Elo</c>, <c>Aura</c>, <c>JCB</c>, <c>Diners</c>, <c>Discover</c>, <c>Hipercard</c>.</summary>
        public string? Brand { get; set; }

        /// <summary>Solicita a tokenizacao do cartao durante a autorizacao.</summary>
        public bool? SaveCard { get; set; }

        /// <summary>Token do cartao previamente salvo na Cielo.</summary>
        public string? CardToken { get; set; }
    }

    /// <summary>
    /// No <c>RecurrentPayment</c> da recorrencia agendada pela Cielo.
    /// </summary>
    public class CieloRecurrentPaymentRequest
    {
        /// <summary>Autoriza a primeira cobranca imediatamente.</summary>
        public bool AuthorizeNow { get; set; } = true;

        /// <summary>Data da primeira cobranca agendada, formato <c>yyyy-MM-dd</c>. Use apenas com <see cref="AuthorizeNow"/> falso.</summary>
        public string? StartDate { get; set; }

        /// <summary>Data de encerramento da recorrencia, formato <c>yyyy-MM-dd</c>.</summary>
        public string? EndDate { get; set; }

        /// <summary>Periodicidade: <c>Monthly</c>, <c>Bimonthly</c>, <c>Quarterly</c>, <c>SemiAnnual</c> ou <c>Annual</c>.</summary>
        public string? Interval { get; set; }
    }
}
