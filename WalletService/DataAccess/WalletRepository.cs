using Microsoft.EntityFrameworkCore;
using WalletService.Data;
using WalletService.Models;

namespace WalletService.DataAccess
{
    public class WalletRepository : IWalletRepository
    {
        private readonly WalletDbContext _context;
        private readonly ILogger<WalletRepository> _logger;
        private readonly IDbContextTransactionProxy _dbContextTransactionProxy;

        public WalletRepository(WalletDbContext context,
            ILogger<WalletRepository> logger,
            IDbContextTransactionProxy dbContextTransactionProxy)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContextTransactionProxy = dbContextTransactionProxy ??
                                         throw new ArgumentNullException(nameof(dbContextTransactionProxy));
        }

        public Task<Wallet> GetWalletByIdAsync(Guid id)
        {
            return _context.Wallets.FirstOrDefaultAsync(w => w.Id == id)!;
        }

        public async Task CreateWalletAsync(Wallet wallet)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));

            wallet.ConcurrencyToken = GenerateNewConcurrencyToken();
            await _context.Wallets.AddAsync(wallet);
            await SaveChangesAsync();
        }

        public async Task UpdateWalletAndCreateTransactionAsync(Wallet wallet, Transaction transaction)
        {
            var transactionScope = await _dbContextTransactionProxy.BeginTransactionAsync();
            try
            {
                ValidateConcurrencyToken(wallet);

                wallet.ConcurrencyToken = GenerateNewConcurrencyToken();

                await UpdateWallet(wallet);
                await CreateTransactionAsync(transaction);

                await _dbContextTransactionProxy.CommitAsync(transactionScope);
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                await _dbContextTransactionProxy.RollbackAsync(transactionScope);
                _logger.LogError(dbEx, "CorrelationId: {CorrelationId} - Concurrency conflict occurred: {Message}",
                    transaction.CorrelationId, dbEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                await _dbContextTransactionProxy.RollbackAsync(transactionScope);
                _logger.LogError(ex,
                    "CorrelationId: {CorrelationId} - An error occurred while updating wallet and creating transaction: {Message}",
                    transaction.CorrelationId, ex.Message);
                throw;
            }
        }

        private void ValidateConcurrencyToken(Wallet wallet)
        {
            var existingWallet = _context.Wallets
                .FirstOrDefault(w => w.Id == wallet.Id && w.ConcurrencyToken == wallet.ConcurrencyToken);

            if (existingWallet == null)
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict occurred.");
            }
        }

        private Guid GenerateNewConcurrencyToken() => Guid.NewGuid();

        private async Task UpdateWallet(Wallet wallet)
        {
            _context.Entry(wallet).State = EntityState.Modified;
            await SaveChangesAsync();
        }

        private async Task CreateTransactionAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await SaveChangesAsync();
        }

        private async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}