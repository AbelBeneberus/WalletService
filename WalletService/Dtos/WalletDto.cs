namespace WalletService.Dtos;

public class WalletDto
{
    public Guid walletId { get; set; }
    public string Owner { get; set; }
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
}