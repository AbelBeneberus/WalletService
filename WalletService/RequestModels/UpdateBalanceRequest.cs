namespace WalletService.RequestModels;

public class UpdateBalanceRequest
{
    public Guid CorrelationId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public Guid ClientId { get; set; }
}