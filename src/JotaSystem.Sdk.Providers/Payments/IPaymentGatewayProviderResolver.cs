namespace JotaSystem.Sdk.Providers.Payments
{
    public interface IPaymentGatewayProviderResolver
    {
        /// <summary>Resolve o gateway pela chave e falha quando ele nao esta registrado.</summary>
        IPaymentGatewayProvider Resolve(string providerKey);

        /// <summary>Resolve o gateway pela chave e devolve nulo quando ele nao esta registrado.</summary>
        IPaymentGatewayProvider? Find(string providerKey);
    }
}
