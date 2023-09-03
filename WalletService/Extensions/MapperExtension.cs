using WalletService.Dtos;
using WalletService.Models;

namespace WalletService.Extensions;

public static class MapperExtension
{
    public static WalletDto ToDto(this Wallet wallet)
    {
        return new WalletDto()
        {
            walletId = wallet.Id,
            Owner = wallet.FullName,
            UserId = wallet.UserId,
            Balance = wallet.Balance
        };
    }
}