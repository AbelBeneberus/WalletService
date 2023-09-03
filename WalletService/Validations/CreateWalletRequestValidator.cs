using FluentValidation;

namespace WalletService.Validations;

using WalletService.RequestModels;

public class CreateWalletRequestValidator : AbstractValidator<CreateWalletRequest>
{
    public CreateWalletRequestValidator()
    {
        RuleFor(x => x.Balance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("The balance of a wallet cannot be negative.");
    }
}