namespace WexCorporatePayments.Domain.Entities;

/// <summary>
/// Represents a purchase transaction in USD.
/// </summary>
public class PurchaseTransaction
{
    public Guid Id { get; private set; }
    
    public string Description { get; private set; } = string.Empty;
    
    public DateTime TransactionDate { get; private set; }
    
    public decimal AmountUsd { get; private set; }

    // Constructor for EF Core
    private PurchaseTransaction() { }

    public PurchaseTransaction(string description, DateTime transactionDate, decimal amountUsd)
    {
        Id = Guid.NewGuid();
        SetDescription(description);
        SetTransactionDate(transactionDate);
        SetAmountUsd(amountUsd);
    }

    private void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new Exceptions.DomainValidationException("Description is required.");

        if (description.Length > 50)
            throw new Exceptions.DomainValidationException("Description cannot exceed 50 characters.");

        Description = description.Trim();
    }

    private void SetTransactionDate(DateTime transactionDate)
    {
        if (transactionDate == default)
            throw new Exceptions.DomainValidationException("Transaction date is required.");

        TransactionDate = transactionDate;
    }

    private void SetAmountUsd(decimal amountUsd)
    {
        if (amountUsd <= 0)
            throw new Exceptions.DomainValidationException("Amount in USD must be greater than zero.");

        // Round to 2 decimal places with MidpointRounding.ToEven
        AmountUsd = Math.Round(amountUsd, 2, MidpointRounding.ToEven);
    }
}
