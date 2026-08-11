using Draya.Domain.Payments;
using Draya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Payments;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentTransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.PaymentTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentTransactions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task UpdateAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        _context.PaymentTransactions.Update(transaction);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
