namespace WexCorporatePayments.Application.DTOs;

/// <summary>
/// Response DTO with currency conversion information.
/// </summary>
public class ConvertedPurchaseResponse
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal AmountUsd { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal ConvertedAmount { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public DateTime RecordDate { get; set; }
}
