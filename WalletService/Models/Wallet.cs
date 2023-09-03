namespace WalletService.Models;

public class Wallet
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
    public string FullName { get; set; }
    public Guid UserId { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
}