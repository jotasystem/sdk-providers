namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models
{
    /// <summary>
    /// Status de uma transacao do Link de Pagamento. Chega como numero nas notificacoes e
    /// como texto nas consultas, por isso e lido pelo <see cref="CieloLinkStatusConverter"/>.
    /// </summary>
    public enum CieloLinkStatusEnum
    {
        /// <summary>Pagamento gerado, aguardando a acao do portador.</summary>
        Pending = 1,

        /// <summary>Transacao capturada.</summary>
        Paid = 2,

        /// <summary>Transacao nao autorizada pelo meio de pagamento.</summary>
        Denied = 3,

        /// <summary>Prazo de autorizacao ou de pagamento vencido.</summary>
        Expired = 4,

        /// <summary>Transacao cancelada pela loja.</summary>
        Voided = 5,

        /// <summary>Falha de processamento que depende do suporte Cielo.</summary>
        NotFinalized = 6,

        /// <summary>Aprovada pelo emissor, ainda sem captura.</summary>
        Authorized = 7,

        /// <summary>Aguardando a autenticacao por biometria facial.</summary>
        AuthorizedIdPayPending = 10
    }
}
