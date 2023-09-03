namespace WalletService.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime TimeStamp { get; set; }
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid ClientId { get; set; }
}