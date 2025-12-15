using WexCorporatePayments.Application.DTOs;
using WexCorporatePayments.Domain.Entities;
using WexCorporatePayments.Domain.Repositories;

namespace WexCorporatePayments.Application.Handlers;

/// <summary>
/// Handler responsible for creating a new purchase transaction.
/// </summary>
public class CreatePurchaseTransactionHandler
{
    private readonly IPurchaseTransactionRepository _repository;

    public CreatePurchaseTransactionHandler(IPurchaseTransactionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Guid> HandleAsync(CreatePurchaseTransactionRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Additional validations (DataAnnotations already validated by controller)
        var transaction = new PurchaseTransaction(
            request.Description,
            request.TransactionDate,
            request.AmountUsd
        );

        await _repository.AddAsync(transaction);

        return transaction.Id;
    }
}
