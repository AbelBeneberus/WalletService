using Microsoft.EntityFrameworkCore;
using WalletService.Models;

namespace WalletService.Data;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
    {
    }

    public virtual DbSet<Wallet> Wallets { get; set; } = null!;
    public virtual DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.UserId)
                .IsUnique();

            entity.HasCheckConstraint("CK_Wallet_Balance", "[Balance] >= 0");

            entity.HasMany<Transaction>(w => w.Transactions)
                .WithOne(t => t.Wallet)
                .HasForeignKey(t => t.WalletId);

            entity.Property(e => e.ConcurrencyToken)
                .IsConcurrencyToken();
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(10,4)");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(t => t.Amount)
                .HasColumnType("decimal(10,4)");
        });
    }
}