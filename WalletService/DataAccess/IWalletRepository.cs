using WalletService.Models;

namespace WalletService.DataAccess;

public interface IWalletRepository
{
    Task<Wallet> GetWalletByIdAsync(Guid id);
    Task CreateWalletAsync(Wallet? wallet);
    Task UpdateWalletAndCreateTransactionAsync(Wallet wallet, Transaction transaction);
}