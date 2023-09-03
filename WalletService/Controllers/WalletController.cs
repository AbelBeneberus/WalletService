using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using WalletService.RequestModels;
using WalletService.Services;

namespace WalletService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly ILogger<WalletController> _logger;
        private readonly IWalletService _walletService;

        public WalletController(ILogger<WalletController> logger, IWalletService walletService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _walletService = walletService ?? throw new ArgumentNullException(nameof(walletService));
        }

        [HttpPost]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest createWalletRequest)
        {
            try
            {
                var result = await _walletService.CreateWalletAsync(createWalletRequest);
                return Accepted(new { Message = "Wallet Created Successfully.", Data = result });
            }
            catch (ValidationException ex)
            {
                var errorMessages = ex.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { message = "Validation Failed", errors = errorMessages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CorrelationId: {CorrelationId} - Failed to create wallet",
                    createWalletRequest.CorrelationId);
                return StatusCode(500, "An error occurred while creating wallet.");
            }
        }

        [HttpGet("{walletId}")]
        public async Task<IActionResult> GetWallet(Guid correlationId, Guid walletId, Guid clientId)
        {
            try
            {
                _logger.LogInformation(
                    "CorrelationId: {CorrelationId} - Wallet with Id: {WalletId} requested from client {ClientId}",
                    correlationId, walletId, clientId);
                var wallet = await _walletService.GetWalletAsync(correlationId, walletId);
                if (wallet == null)
                {
                    return NotFound("Wallet not found.");
                }

                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CorrelationId: {CorrelationId} - Failed to retrieve the wallet for client {ClientId}",
                    correlationId, clientId);
                return StatusCode(500, "An error occurred while retrieving the wallet.");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateWallet([FromBody] UpdateBalanceRequest updateBalanceRequest)
        {
            try
            {
                var result = await _walletService.UpdateWalletAndCreateTransactionAsync(updateBalanceRequest);
                return Accepted(new { Message = "Wallet updated successfully.", Data = result });
            }
            catch (ValidationException ex)
            {
                var errorMessages = ex.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { message = "Validation Failed", errors = errorMessages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CorrelationId: {CorrelationId} - Failed to update the wallet",
                    updateBalanceRequest.CorrelationId);
                return StatusCode(500, "An error occurred while updating the wallet.");
            }
        }
    }
}