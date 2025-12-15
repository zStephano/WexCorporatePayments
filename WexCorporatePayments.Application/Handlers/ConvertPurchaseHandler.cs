using WexCorporatePayments.Application.DTOs;
using WexCorporatePayments.Application.Services;
using WexCorporatePayments.Domain.Repositories;

namespace WexCorporatePayments.Application.Handlers;

/// <summary>
/// Handler responsible for converting a purchase to a foreign currency.
/// </summary>
public class ConvertPurchaseHandler
{
    private readonly IPurchaseTransactionRepository _repository;
    private readonly IExchangeRateService _exchangeRateService;

    public ConvertPurchaseHandler(
        IPurchaseTransactionRepository repository,
        IExchangeRateService exchangeRateService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
    }

    public async Task<ConvertedPurchaseResponse?> HandleAsync(Guid transactionId, string country, string currency)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required.", nameof(country));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        // Fetch the transaction
        var transaction = await _repository.GetByIdAsync(transactionId);
        if (transaction == null)
            return null;

        // Fetch exchange rate with rules: record_date <= TransactionDate and >= TransactionDate - 6 months
        var exchangeRateResult = await _exchangeRateService.GetLatestRateAsync(
            country, 
            currency, 
            transaction.TransactionDate);

        if (exchangeRateResult == null)
        {
            throw new InvalidOperationException(
                $"Could not find a valid exchange rate for {country}/{currency} " +
                $"within the last 6 months of the transaction date ({transaction.TransactionDate:yyyy-MM-dd}).");
        }

        // Validate if rate is within 6-month window
        var sixMonthsAgo = transaction.TransactionDate.AddMonths(-6);
        if (exchangeRateResult.RecordDate < sixMonthsAgo || exchangeRateResult.RecordDate > transaction.TransactionDate)
        {
            throw new InvalidOperationException(
                $"The exchange rate found (record_date: {exchangeRateResult.RecordDate:yyyy-MM-dd}) " +
                $"is outside the valid period (between {sixMonthsAgo:yyyy-MM-dd} and {transaction.TransactionDate:yyyy-MM-dd}).");
        }

        // Calculate converted value and round to 2 decimal places
        var convertedAmount = Math.Round(
            transaction.AmountUsd * exchangeRateResult.ExchangeRate, 
            2, 
            MidpointRounding.ToEven);

        return new ConvertedPurchaseResponse
        {
            Id = transaction.Id,
            Description = transaction.Description,
            TransactionDate = transaction.TransactionDate,
            AmountUsd = transaction.AmountUsd,
            ExchangeRate = exchangeRateResult.ExchangeRate,
            ConvertedAmount = convertedAmount,
            Country = exchangeRateResult.Country,
            Currency = exchangeRateResult.Currency,
            RecordDate = exchangeRateResult.RecordDate
        };
    }
}
