using FluentValidation;
using WalletService.Models;
using WalletService.RequestModels;

namespace WalletService.Validations;

public class UpdateWalletRequestValidator : AbstractValidator<UpdateBalanceRequest>
{
    public UpdateWalletRequestValidator()
    {
        RuleFor(x => x)
            .Custom((updateWalletRequest, context) =>
            {
                var wallet = context.RootContextData["Wallet"] as Wallet;
                if (wallet == null)
                {
                    context.AddFailure("The wallet does not exist.");
                }
                else if (wallet.Balance + updateWalletRequest.Amount < 0)
                {
                    context.AddFailure("Insufficient fund.");
                }
            });
    }
}