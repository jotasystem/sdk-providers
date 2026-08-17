using JotaSystem.Sdk.Core.CrossCutting.Providers.Enum;
using JotaSystem.Sdk.Core.CrossCutting.Providers.Models;
using JotaSystem.Sdk.Providers.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class PaymentGatewayProviderResolverTests
    {
        [Fact]
        public void Resolve_ShouldReturnRegisteredProvider_IgnoringCase()
        {
            var services = new ServiceCollection();
            services.AddScoped<IPaymentGatewayProvider, FakePaymentGatewayProvider>();
            services.AddJotaSystemProviders().AddPayments();

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var resolver = scope.ServiceProvider.GetRequiredService<IPaymentGatewayProviderResolver>();
            var provider = resolver.Resolve(" FAKE ");

            Assert.IsType<FakePaymentGatewayProvider>(provider);
        }

        [Fact]
        public void Resolve_ShouldThrow_WhenProviderIsNotRegistered()
        {
            var services = new ServiceCollection();
            services.AddJotaSystemProviders().AddPayments();

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var resolver = scope.ServiceProvider.GetRequiredService<IPaymentGatewayProviderResolver>();

            var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("missing"));

            Assert.Contains("missing", exception.Message);
        }

        private sealed class FakePaymentGatewayProvider : IPaymentGatewayProvider
        {
            public string ProviderKey => "fake";

            public Task<PaymentProviderResult> CreateAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default) =>
                Task.FromResult(Success());

            public Task<PaymentProviderResult> GetAsync(PaymentProviderQuery query, CancellationToken cancellationToken = default) =>
                Task.FromResult(Success());

            public Task<PaymentProviderResult> CancelAsync(PaymentProviderOperation operation, CancellationToken cancellationToken = default) =>
                Task.FromResult(Success(PaymentProviderStatusEnum.Cancelled));

            public Task<PaymentProviderResult> RefundAsync(PaymentProviderRefund operation, CancellationToken cancellationToken = default) =>
                Task.FromResult(Success(PaymentProviderStatusEnum.Refunded));

            public Task<PaymentWebhookEvent> ParseWebhookAsync(PaymentWebhookRequest request, CancellationToken cancellationToken = default) =>
                Task.FromResult(new PaymentWebhookEvent(
                    ProviderKey,
                    "event-id",
                    "payment.paid",
                    "transaction-id",
                    PaymentProviderStatusEnum.Paid,
                    10m,
                    "BRL",
                    DateTimeOffset.UtcNow,
                    request.Payload));

            private static PaymentProviderResult Success(PaymentProviderStatusEnum status = PaymentProviderStatusEnum.Pending) =>
                new(true, status, "transaction-id");
        }
    }
}
