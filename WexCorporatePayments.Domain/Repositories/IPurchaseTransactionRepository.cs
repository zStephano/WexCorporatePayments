using WexCorporatePayments.Domain.Entities;

namespace WexCorporatePayments.Domain.Repositories;

/// <summary>
/// Repository contract for PurchaseTransaction.
/// </summary>
public interface IPurchaseTransactionRepository
{
    Task AddAsync(PurchaseTransaction transaction);
    Task<PurchaseTransaction?> GetByIdAsync(Guid id);
}
