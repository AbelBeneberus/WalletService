namespace WalletService.RequestModels;

public class CreateWalletRequest
{
    public Guid CorrelationId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public decimal Balance { get; set; }
}