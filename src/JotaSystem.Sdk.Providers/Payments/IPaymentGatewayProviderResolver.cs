namespace JotaSystem.Sdk.Providers.Payments
{
    public interface IPaymentGatewayProviderResolver
    {
        IPaymentGatewayProvider Resolve(string providerKey);
    }
}
