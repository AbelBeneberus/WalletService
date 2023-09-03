using WalletService.Dtos;
using WalletService.RequestModels;

namespace WalletService.Services;

public interface IWalletService
{
    Task<WalletDto> CreateWalletAsync(CreateWalletRequest createWalletRequest);
    Task<WalletDto> GetWalletAsync(Guid correlationId, Guid walletId);
    Task<WalletDto> UpdateWalletAndCreateTransactionAsync(UpdateBalanceRequest updateBalanceRequest);
}