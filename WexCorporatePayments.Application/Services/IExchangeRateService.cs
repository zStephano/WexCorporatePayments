namespace WexCorporatePayments.Application.Services;

/// <summary>
/// Exchange rate result from Treasury API.
/// </summary>
public class ExchangeRateResult
{
    public string Country { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime RecordDate { get; set; }
}

/// <summary>
/// Contract for exchange rate query service.
/// </summary>
public interface IExchangeRateService
{
    Task<ExchangeRateResult?> GetLatestRateAsync(string country, string currency, DateTime purchaseDate);
}
