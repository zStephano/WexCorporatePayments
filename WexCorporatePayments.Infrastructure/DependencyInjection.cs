using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WexCorporatePayments.Application.Services;
using WexCorporatePayments.Domain.Repositories;
using WexCorporatePayments.Infrastructure.ExternalServices;
using WexCorporatePayments.Infrastructure.Persistence;

namespace WexCorporatePayments.Infrastructure;

/// <summary>
/// DI configuration for Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure DbContext with SQLite
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=wex.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register repositories
        services.AddScoped<IPurchaseTransactionRepository, PurchaseTransactionRepository>();

        // Configure HttpClient for ExchangeRateService
        services.AddHttpClient<IExchangeRateService, ExchangeRateService>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = config["TreasuryApi:BaseUrl"] 
                    ?? "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/";
                
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
