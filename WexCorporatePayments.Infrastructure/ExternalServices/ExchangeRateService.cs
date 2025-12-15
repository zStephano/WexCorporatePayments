using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WexCorporatePayments.Application.Services;

namespace WexCorporatePayments.Infrastructure.ExternalServices;

/// <summary>
/// Treasury API exchange rate query service implementation.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly string _baseUrl;

    public ExchangeRateService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ExchangeRateService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _baseUrl = configuration["TreasuryApi:BaseUrl"] 
            ?? "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/";
    }

    public async Task<ExchangeRateResult?> GetLatestRateAsync(string country, string currency, DateTime purchaseDate)
    {
        try
        {
            // Calculate period of 6 months ago
            var sixMonthsAgo = purchaseDate.AddMonths(-6);

            // Build URL with filters
            var endpoint = "v1/accounting/od/rates_of_exchange";
            var fields = "country,currency,exchange_rate,record_date";
            var filter = $"country:eq:{country},currency:eq:{currency}," +
                        $"record_date:lte:{purchaseDate:yyyy-MM-dd}," +
                        $"record_date:gte:{sixMonthsAgo:yyyy-MM-dd}";
            var sort = "-record_date"; // Sort from newest to oldest
            var pageSize = "1";

            var url = $"{_baseUrl}{endpoint}?fields={fields}&filter={filter}&sort={sort}&page[size]={pageSize}";

            _logger.LogInformation("Querying exchange rate: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to query Treasury API. Status: {StatusCode}", 
                    response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            
            var treasuryResponse = JsonSerializer.Deserialize<TreasuryApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (treasuryResponse?.Data == null || !treasuryResponse.Data.Any())
            {
                _logger.LogInformation(
                    "No rate found for {Country}/{Currency} between {StartDate} and {EndDate}",
                    country, currency, sixMonthsAgo.ToString("yyyy-MM-dd"), purchaseDate.ToString("yyyy-MM-dd"));
                return null;
            }

            var data = treasuryResponse.Data.First();

            return new ExchangeRateResult
            {
                Country = data.Country,
                Currency = data.Currency,
                ExchangeRate = decimal.Parse(data.ExchangeRate),
                RecordDate = DateTime.Parse(data.RecordDate)
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error querying Treasury API");
            throw new InvalidOperationException("Error querying exchange rate service.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error querying Treasury API");
            throw;
        }
    }

    // Helper classes for deserialization
    private class TreasuryApiResponse
    {
        public List<TreasuryDataItem> Data { get; set; } = new();
    }

    private class TreasuryDataItem
    {
        public string Country { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string ExchangeRate { get; set; } = string.Empty;
        public string RecordDate { get; set; } = string.Empty;
    }
}
