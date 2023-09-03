using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using WalletService.DataAccess;
using WalletService.Dtos;
using WalletService.Extensions;
using WalletService.Models;
using WalletService.RequestModels;

namespace WalletService.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly ILogger<WalletService> _logger;
    private readonly IValidator<CreateWalletRequest> _createWalletValidator;
    private readonly IValidator<UpdateBalanceRequest> _updateWalletValidator;


    public WalletService(IWalletRepository walletRepository,
        ILogger<WalletService> logger,
        IValidator<CreateWalletRequest> createWalletValidator,
        IValidator<UpdateBalanceRequest> updateWalletValidator)
    {
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createWalletValidator =
            createWalletValidator ?? throw new ArgumentNullException(nameof(createWalletValidator));
        _updateWalletValidator =
            updateWalletValidator ?? throw new ArgumentNullException(nameof(updateWalletValidator));
    }

    public async Task<WalletDto> CreateWalletAsync(CreateWalletRequest createWalletRequest)
    {
        try
        {
            var validationResult = await _createWalletValidator.ValidateAsync(createWalletRequest);
            if (!validationResult.IsValid)
            {
                _logger.LogError(
                    "CorrelationId: {CorrelationId} -  Validation failed for CreateWalletRequest: {ValidationErrors}",
                    createWalletRequest.CorrelationId,
                    validationResult.Errors);
                throw new ValidationException("Validation failed", validationResult.Errors);
            }

            var wallet = new Wallet()
            {
                CorrelationId = createWalletRequest.CorrelationId,
                UserId = createWalletRequest.UserId,
                Id = Guid.NewGuid(),
                Balance = createWalletRequest.Balance,
                FullName = createWalletRequest.FullName
            };

            await _walletRepository.CreateWalletAsync(wallet);
            _logger.LogInformation(
                "CorrelationId: {CorrelationId} - Successfully created wallet for User: {UserId} with WalletId: {WalletId}",
                createWalletRequest.CorrelationId,
                createWalletRequest.UserId,
                wallet.Id);
            return wallet.ToDto();
        }
        catch (DbUpdateException)
        {
            var validationFailure = new ValidationFailure("UserId", "A wallet with the same userId already exists");
            throw new ValidationException("Unable to update the db.", new[] { validationFailure });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CorrelationId: {CorrelationId} - An error occurred while creating wallet for User: {UserId} Message: {Message}",
                createWalletRequest.CorrelationId,
                createWalletRequest.UserId,
                ex.Message);
            throw;
        }
    }

    public async Task<WalletDto> GetWalletAsync(Guid correlationId, Guid walletId)
    {
        try
        {
            var wallet = await _walletRepository.GetWalletByIdAsync(walletId);
            if (wallet == null)
            {
                _logger.LogWarning("CorrelationId: {CorrelationId} - Wallet with ID {WalletId} not found",
                    correlationId, walletId);
                return null;
            }

            return wallet.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CorrelationId: {CorrelationId} - An error occurred while retrieving wallet with ID {WalletId}",
                correlationId, walletId);
            throw;
        }
    }

    public async Task<WalletDto> UpdateWalletAndCreateTransactionAsync(UpdateBalanceRequest updateBalanceRequest)
    {
        var policy = CreateConcurrencyRetryPolicy();

        var contextData = new Context()
        {
            { "CorrelationId", updateBalanceRequest.CorrelationId },
            { "WalletId", updateBalanceRequest.WalletId }
        };
        return await policy.ExecuteAsync(async context =>
        {
            var walletId = Guid.Parse(context["WalletId"].ToString()!);
            var wallet = context.TryGetValue("Wallet", out var value)
                ? value as Wallet
                : await _walletRepository.GetWalletByIdAsync(walletId);

            await ValidateUpdateWalletRequest(updateBalanceRequest, wallet);

            var transaction = new Transaction()
            {
                CorrelationId = updateBalanceRequest.CorrelationId,
                WalletId = wallet.Id,
                Amount = updateBalanceRequest.Amount,
                TimeStamp = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                ClientId = updateBalanceRequest.ClientId
            };

            wallet.Balance += updateBalanceRequest.Amount;
            await _walletRepository.UpdateWalletAndCreateTransactionAsync(wallet, transaction);

            _logger.LogInformation(
                "CorrelationId: {CorrelationId} - Successfully updated wallet and created transaction for Wallet: {WalletId} Transaction: {TransactionId}",
                updateBalanceRequest.CorrelationId,
                wallet.Id,
                transaction.Id);

            return wallet.ToDto();
        }, contextData);
    }

    private AsyncRetryPolicy CreateConcurrencyRetryPolicy()
    {
        return Policy.Handle<DbUpdateConcurrencyException>()
            .RetryAsync(3, async (exception, retryCount, context) =>
            {
                var correlationId = context["CorrelationId"].ToString();
                _logger.LogWarning(
                    "CorrelationId: {CorrelationId} - Retrying due to concurrency conflict. Retry attempt {RetryCount}",
                    correlationId, retryCount);

                var walletId = Guid.Parse(context["WalletId"].ToString()!);
                var wallet = await _walletRepository.GetWalletByIdAsync(walletId);
                context["Wallet"] = wallet;
            });
    }

    private async Task ValidateUpdateWalletRequest(UpdateBalanceRequest updateBalanceRequest, Wallet wallet)
    {
        var validationContext = new ValidationContext<UpdateBalanceRequest>(updateBalanceRequest)
        {
            RootContextData = { ["Wallet"] = wallet }
        };
        var validationResult = await _updateWalletValidator.ValidateAsync(validationContext);

        if (!validationResult.IsValid)
        {
            _logger.LogError(
                "CorrelationId: {CorrelationId} - Validation failed for UpdateBalanceRequest: {ValidationErrors} for walletId: {WalletId}",
                updateBalanceRequest.CorrelationId,
                validationResult.Errors,
                updateBalanceRequest.WalletId);

            throw new ValidationException("Validation failed", validationResult.Errors);
        }
    }
}