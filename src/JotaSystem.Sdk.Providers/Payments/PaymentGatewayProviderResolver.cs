namespace JotaSystem.Sdk.Providers.Payments
{
    internal sealed class PaymentGatewayProviderResolver(IEnumerable<IPaymentGatewayProvider> providers)
        : IPaymentGatewayProviderResolver
    {
        public IPaymentGatewayProvider Resolve(string providerKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);

            var provider = providers.SingleOrDefault(x =>
                string.Equals(x.ProviderKey, providerKey.Trim(), StringComparison.OrdinalIgnoreCase));

            return provider ?? throw new InvalidOperationException(
                $"Payment gateway provider '{providerKey}' is not registered.");
        }
    }
}
