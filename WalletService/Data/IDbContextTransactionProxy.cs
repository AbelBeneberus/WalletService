using Microsoft.EntityFrameworkCore.Storage;

namespace WalletService.Data;

/// <summary>
/// Provides an abstraction over <see cref="IDbContextTransaction"/> to facilitate unit testing
/// and decouple the application code from database-specific implementations.
/// </summary>
/// <remarks>
/// This interface is particularly useful for testability, as it allows you to mock transaction
/// behaviors during unit testing.
/// </remarks>
public interface IDbContextTransactionProxy
{
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitAsync(IDbContextTransaction transaction);
    Task RollbackAsync(IDbContextTransaction transaction);
}