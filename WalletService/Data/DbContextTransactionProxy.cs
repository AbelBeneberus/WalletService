using Microsoft.EntityFrameworkCore.Storage;

namespace WalletService.Data;

public class DbContextTransactionProxy : IDbContextTransactionProxy
{
    private readonly WalletDbContext _context;

    public DbContextTransactionProxy(WalletDbContext context)
    {
        _context = context;
    }

    public virtual Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }

    public virtual Task CommitAsync(IDbContextTransaction transaction)
    {
        return transaction.CommitAsync();
    }

    public virtual Task RollbackAsync(IDbContextTransaction transaction)
    {
        return transaction.RollbackAsync();
    }
}