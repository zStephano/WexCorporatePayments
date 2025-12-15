using Microsoft.Extensions.DependencyInjection;
using WexCorporatePayments.Application.Handlers;

namespace WexCorporatePayments.Application;

/// <summary>
/// DI configuration for Application layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register handlers
        services.AddScoped<CreatePurchaseTransactionHandler>();
        services.AddScoped<ConvertPurchaseHandler>();

        return services;
    }
}
