using Microsoft.EntityFrameworkCore;
using WexCorporatePayments.Domain.Entities;
using WexCorporatePayments.Domain.Repositories;

namespace WexCorporatePayments.Infrastructure.Persistence;

/// <summary>
/// PurchaseTransaction repository implementation using EF Core.
/// </summary>
public class PurchaseTransactionRepository : IPurchaseTransactionRepository
{
    private readonly AppDbContext _context;

    public PurchaseTransactionRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(PurchaseTransaction transaction)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        await _context.PurchaseTransactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<PurchaseTransaction?> GetByIdAsync(Guid id)
    {
        return await _context.PurchaseTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
