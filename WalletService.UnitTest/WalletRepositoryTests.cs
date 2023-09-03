using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using WalletService.Data;
using WalletService.Models;
using WalletService.DataAccess;
using Xunit;

namespace WalletService.UnitTest
{
    public class WalletRepositoryTests
    {
        private readonly WalletDbContext _dbContext;
        private readonly WalletRepository _walletRepository;
        private readonly ILogger<WalletRepository> _logger;

        private readonly Mock<DbContextTransactionProxy> _dbContextTransactionProxyMock;

        public WalletRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _dbContext = new WalletDbContext(options);
            _logger = new LoggerFactory().CreateLogger<WalletRepository>();
            _dbContextTransactionProxyMock =
                new Mock<DbContextTransactionProxy>(MockBehavior.Loose, _dbContext);

            _walletRepository = new WalletRepository(_dbContext, _logger, _dbContextTransactionProxyMock.Object);
        }

        [Fact]
        public async Task GetWalletByIdAsync_ShouldReturnWallet_WhenValidIdIsProvided()
        {
            // Arrange
            var wallet = GetWallet();
            _dbContext.Wallets.Add(wallet);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _walletRepository.GetWalletByIdAsync(wallet.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(wallet.Id);
        }

        [Fact]
        public async Task CreateWalletAsync_ShouldAddWalletToContextAndSaveChanges()
        {
            // Arrange
            var wallet = GetWallet();

            // Act
            await _walletRepository.CreateWalletAsync(wallet);

            // Assert
            _dbContext.Wallets.Should().Contain(wallet);
        }

        [Fact]
        public async Task UpdateWalletAndCreateTransactionAsync_ShouldUpdateWalletAndCreateTransaction()
        {
            // Arrange
            var wallet = GetWallet();
            var transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                Amount = 10,
                WalletId = wallet.Id,
                TimeStamp = DateTime.UtcNow
            };

            _dbContext.Wallets.Add(wallet);
            await _dbContext.SaveChangesAsync();

            var dbContextTransactionMock = new Mock<IDbContextTransaction>();

            _dbContextTransactionProxyMock.Setup(proxy => proxy.BeginTransactionAsync())
                .ReturnsAsync(dbContextTransactionMock.Object);
            _dbContextTransactionProxyMock.Setup(proxy => proxy.CommitAsync(dbContextTransactionMock.Object))
                .Returns(Task.CompletedTask);
            // Act
            await _walletRepository.UpdateWalletAndCreateTransactionAsync(wallet, transaction);

            // Assert
            _dbContextTransactionProxyMock.Verify(proxy => proxy.BeginTransactionAsync(), Times.Once);
            _dbContextTransactionProxyMock.Verify(proxy => proxy.CommitAsync(dbContextTransactionMock.Object),
                Times.Once);
        }

        [Fact]
        public async Task UpdateWalletAndCreateTransactionAsync_ShouldThrowConcurrencyException()
        {
            // Arrange
            var wallet = GetWallet();
            _dbContext.Wallets.Add(wallet);
            await _dbContext.SaveChangesAsync();
            var transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                Amount = 10,
                WalletId = wallet.Id,
                TimeStamp = DateTime.UtcNow  ,
                ClientId = Guid.NewGuid()
            };

            var dbContextTransactionMock = new Mock<IDbContextTransaction>();
            _dbContextTransactionProxyMock.Setup(proxy => proxy.BeginTransactionAsync())
                .ReturnsAsync(dbContextTransactionMock.Object);

            _dbContext.Entry(wallet).State = EntityState.Detached;
            var conflictingWallet = GetWallet();
            conflictingWallet.Id = wallet.Id;

            // Act
            Func<Task> act = async () =>
                await _walletRepository.UpdateWalletAndCreateTransactionAsync(conflictingWallet, transaction);

            // Assert
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>()
                .WithMessage("Concurrency conflict occurred.");

            _dbContextTransactionProxyMock.Verify(proxy => proxy.RollbackAsync(dbContextTransactionMock.Object),
                Times.Once);
        }

        private Wallet GetWallet()
        {
            return new Wallet
            {
                Id = Guid.NewGuid(),
                ConcurrencyToken = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Balance = 5,
                CorrelationId = Guid.NewGuid(),
                FullName = "test"
            };
        }
    }
}