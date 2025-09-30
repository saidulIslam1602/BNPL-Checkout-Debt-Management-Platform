using Microsoft.EntityFrameworkCore;
using YourCompanyBNPL.Settlement.API.Models;

namespace YourCompanyBNPL.Settlement.API.Data;

public class SettlementDbContext : DbContext
{
    public SettlementDbContext(DbContextOptions<SettlementDbContext> options) : base(options)
    {
    }

    public DbSet<SettlementTransaction> Settlements => Set<SettlementTransaction>();
    public DbSet<MerchantAccount> MerchantAccounts => Set<MerchantAccount>();
    public DbSet<SettlementBatch> SettlementBatches => Set<SettlementBatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Settlement Transaction configuration
        modelBuilder.Entity<SettlementTransaction>(entity =>
        {
            entity.ToTable("SettlementTransactions");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Reference)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Reference).IsUnique();
        });

        // Merchant Account configuration
        modelBuilder.Entity<MerchantAccount>(entity =>
        {
            entity.ToTable("MerchantAccounts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MerchantName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.BankAccountNumber)
                .HasMaxLength(11)
                .IsRequired();

            entity.Property(e => e.BankName)
                .HasMaxLength(100);

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.HasIndex(e => e.MerchantId).IsUnique();
            entity.HasIndex(e => e.BankAccountNumber);
        });

        // Settlement Batch configuration
        modelBuilder.Entity<SettlementBatch>(entity =>
        {
            entity.ToTable("SettlementBatches");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.BatchReference)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => e.BatchReference).IsUnique();
            entity.HasIndex(e => e.ProcessedAt);
        });
    }
}