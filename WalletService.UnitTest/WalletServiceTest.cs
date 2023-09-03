using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WalletService.DataAccess;
using WalletService.Models;
using WalletService.RequestModels;
using Xunit;

namespace WalletService.UnitTest
{
    public class WalletServiceTests
    {
        private readonly Mock<IWalletRepository> _walletRepositoryMock = new();
        private readonly Mock<ILogger<Services.WalletService>> _loggerMock = new();
        private readonly Mock<IValidator<CreateWalletRequest>> _createWalletValidatorMock = new();
        private readonly Mock<IValidator<UpdateBalanceRequest>> _updateWalletValidatorMock = new();

        [Fact]
        public async void CreateWalletAsync_ValidRequest_ShouldReturnWalletDto()
        {
            // Arrange
            var request = new CreateWalletRequest
            {
                CorrelationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FullName = "Test User",
                Balance = 100
            };

            var walletService = new WalletService.Services.WalletService(
                _walletRepositoryMock.Object,
                _loggerMock.Object,
                _createWalletValidatorMock.Object,
                _updateWalletValidatorMock.Object);

            _createWalletValidatorMock.Setup(x => x.ValidateAsync(request, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _walletRepositoryMock.Setup(x => x.CreateWalletAsync(It.IsAny<Wallet>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await walletService.CreateWalletAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Owner.Should().Be(request.FullName);
            result.Balance.Should().Be(request.Balance);
            result.UserId.Should().Be(request.UserId);
        }

        [Fact]
        public async void UpdateWalletAndCreateTransactionAsync_ConcurrencyException_ShouldRetry()
        {
            // Arrange
            int retryCount = 0;
            var request = new UpdateBalanceRequest
            {
                CorrelationId = Guid.NewGuid(),
                WalletId = Guid.NewGuid(),
                Amount = 50,
                ClientId = Guid.NewGuid()
            };

            var walletService = new WalletService.Services.WalletService(
                _walletRepositoryMock.Object,
                _loggerMock.Object,
                _createWalletValidatorMock.Object,
                _updateWalletValidatorMock.Object);

            _updateWalletValidatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<UpdateBalanceRequest>>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _walletRepositoryMock.Setup(x => x.GetWalletByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new Wallet { Id = request.WalletId, Balance = 100 });

            _walletRepositoryMock.Setup(x =>
                    x.UpdateWalletAndCreateTransactionAsync(It.IsAny<Wallet>(), It.IsAny<Transaction>()))
                .Callback(() => retryCount++)
                .Throws<DbUpdateConcurrencyException>();

            // Act
            Func<Task> act = async () => await walletService.UpdateWalletAndCreateTransactionAsync(request);

            // Assert
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
            retryCount.Should().Be(4); // 1 initial try + 3 retries
        }
    }
}