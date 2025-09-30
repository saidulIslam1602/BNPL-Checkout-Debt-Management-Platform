using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using YourCompanyBNPL.Payment.API.Models;
using System.Text.Json;

namespace YourCompanyBNPL.Payment.API.Data;

/// <summary>
/// Database context for the Payment service
/// </summary>
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Models.Payment> Payments { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerAddress> CustomerAddresses { get; set; }
    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<MerchantAddress> MerchantAddresses { get; set; }
    public DbSet<BNPLPlan> BNPLPlans { get; set; }
    public DbSet<Installment> Installments { get; set; }
    public DbSet<PaymentRefund> PaymentRefunds { get; set; }
    public DbSet<PaymentEvent> PaymentEvents { get; set; }
    public DbSet<Settlement> Settlements { get; set; }
    public DbSet<SettlementBatch> SettlementBatches { get; set; }
    public DbSet<SettlementTransaction> SettlementTransactions { get; set; }
    public DbSet<SettlementEvent> SettlementEvents { get; set; }
    public DbSet<SettlementSchedule> SettlementSchedules { get; set; }
    public DbSet<SettlementItem> SettlementItems { get; set; }
    
    // Enhanced Payment API entities
    public DbSet<PaymentToken> PaymentTokens { get; set; }
    public DbSet<WebhookEndpoint> WebhookEndpoints { get; set; }
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; }
    public DbSet<WebhookLog> WebhookLogs { get; set; }
    public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }
    public DbSet<FraudAssessment> FraudAssessments { get; set; }
    public DbSet<FraudReport> FraudReports { get; set; }
    public DbSet<FraudRule> FraudRules { get; set; }
    
    // Alias for BNPL installments to avoid conflicts
    public DbSet<Installment> BNPLInstallments => Installments;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Payment entity
        modelBuilder.Entity<Models.Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.OrderReference);
            entity.HasIndex(e => e.TransactionId).IsUnique();
            
            entity.Property(e => e.Currency)
                .HasConversion<int>();
            
            entity.Property(e => e.Status)
                .HasConversion<int>();
            
            entity.Property(e => e.PaymentMethod)
                .HasConversion<int>();
            
            entity.Property(e => e.TransactionType)
                .HasConversion<int>();

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

            // Relationships
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Payments)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.Payments)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.SocialSecurityNumber).IsUnique();
            entity.HasIndex(e => e.RiskLevel);
            entity.HasIndex(e => e.CreditRating);
            entity.HasIndex(e => e.CollectionStatus);
            
            entity.Property(e => e.RiskLevel)
                .HasConversion<int>();
            
            entity.Property(e => e.CreditRating)
                .HasConversion<int>();
            
            entity.Property(e => e.CollectionStatus)
                .HasConversion<int>();

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
        });

        // Configure CustomerAddress entity
        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            
            entity.HasOne(e => e.Customer)
                .WithOne(c => c.Address)
                .HasForeignKey<CustomerAddress>(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Merchant entity
        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.BusinessRegistrationNumber).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsVerified);

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
        });

        // Configure MerchantAddress entity
        modelBuilder.Entity<MerchantAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MerchantId);
            
            entity.HasOne(e => e.Merchant)
                .WithOne(m => m.Address)
                .HasForeignKey<MerchantAddress>(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BNPLPlan entity
        modelBuilder.Entity<BNPLPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PaymentId).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PlanType);
            
            entity.Property(e => e.Currency)
                .HasConversion<int>();
            
            entity.Property(e => e.PlanType)
                .HasConversion<int>();
            
            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

            // Relationships
            entity.HasOne(e => e.Payment)
                .WithOne(p => p.BNPLPlan)
                .HasForeignKey<BNPLPlan>(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.BNPLPlans)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Merchant)
                .WithMany()
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Installment entity
        modelBuilder.Entity<Installment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BNPLPlanId);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsOverdue);
            entity.HasIndex(e => new { e.BNPLPlanId, e.InstallmentNumber }).IsUnique();
            
            entity.Property(e => e.Status)
                .HasConversion<int>();
            
            entity.Property(e => e.PaymentMethod)
                .HasConversion<int>();

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

            // Relationships
            entity.HasOne(e => e.BNPLPlan)
                .WithMany(p => p.Installments)
                .HasForeignKey(e => e.BNPLPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PaymentRefund entity
        modelBuilder.Entity<PaymentRefund>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.Status);
            
            entity.Property(e => e.Currency)
                .HasConversion<int>();
            
            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

            // Relationships
            entity.HasOne(e => e.Payment)
                .WithMany(p => p.Refunds)
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PaymentEvent entity
        modelBuilder.Entity<PaymentEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.Property(e => e.FromStatus)
                .HasConversion<int>();
            
            entity.Property(e => e.ToStatus)
                .HasConversion<int>();

            entity.Property(e => e.EventData)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

            // Relationships
            entity.HasOne(e => e.Payment)
                .WithMany(p => p.Events)
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Settlement entity
        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.SettlementDate);
            entity.HasIndex(e => e.Status);
            
            entity.Property(e => e.Currency)
                .HasConversion<int>();
            
            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

            // Relationships
            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.Settlements)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure SettlementTransaction entity
        modelBuilder.Entity<SettlementTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SettlementId);
            entity.HasIndex(e => e.PaymentId);
            
            // Relationships
            entity.HasOne(e => e.Settlement)
                .WithMany(s => s.Transactions)
                .HasForeignKey(e => e.SettlementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure enhanced entities
        ConfigureEnhancedEntities(modelBuilder);

        // Seed data
        SeedData(modelBuilder);
    }

    private static void ConfigureEnhancedEntities(ModelBuilder modelBuilder)
    {
        // Configure PaymentToken entity
        modelBuilder.Entity<PaymentToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.PaymentMethod);
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.Property(e => e.PaymentMethod)
                .HasConversion<int>();

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.PaymentTokens)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WebhookEndpoint entity
        modelBuilder.Entity<WebhookEndpoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.IsActive);
            
            entity.Property(e => e.Events)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.WebhookEndpoints)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WebhookDelivery entity
        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WebhookEndpointId);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.NextRetryAt);

            entity.HasOne(e => e.WebhookEndpoint)
                .WithMany(w => w.Deliveries)
                .HasForeignKey(e => e.WebhookEndpointId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure WebhookLog entity
        modelBuilder.Entity<WebhookLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.ProcessedAt);

            entity.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure IdempotencyRecord entity
        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });

        // Configure FraudAssessment entity
        modelBuilder.Entity<FraudAssessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.RiskLevel);
            entity.HasIndex(e => e.AssessedAt);
            
            entity.Property(e => e.PaymentMethod)
                .HasConversion<int>();
            
            entity.Property(e => e.RiskLevel)
                .HasConversion<int>();

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.FraudAssessments)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FraudReport entity
        modelBuilder.Entity<FraudReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ReportedAt);
            
            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.FraudReports)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure FraudRule entity
        modelBuilder.Entity<FraudRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
        });

        // Configure SettlementBatch entity
        modelBuilder.Entity<SettlementBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.BatchReference).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SettlementDate);
            
            entity.Property(e => e.Currency)
                .HasConversion<int>();
            
            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.SettlementBatches)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure SettlementItem entity
        modelBuilder.Entity<SettlementItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SettlementBatchId);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.RefundId);
            
            entity.Property(e => e.TransactionType)
                .HasConversion<int>();

            entity.HasOne(e => e.SettlementBatch)
                .WithMany(sb => sb.Items)
                .HasForeignKey(e => e.SettlementBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Refund)
                .WithMany()
                .HasForeignKey(e => e.RefundId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Update Payment entity configuration for new properties
        modelBuilder.Entity<Models.Payment>(entity =>
        {
            entity.Property(e => e.SettlementStatus)
                .HasConversion<int>();

            entity.HasOne(e => e.PaymentToken)
                .WithMany(pt => pt.Payments)
                .HasForeignKey(e => e.PaymentTokenId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Update PaymentRefund entity configuration for new properties
        modelBuilder.Entity<PaymentRefund>(entity =>
        {
            entity.Property(e => e.SettlementStatus)
                .HasConversion<int>();
        });
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed some test merchants
        var testMerchant1 = new Merchant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "TechStore Norway",
            Email = "merchant@techstore.no",
            PhoneNumber = "+4712345678",
            BusinessRegistrationNumber = "NO123456789",
            VATNumber = "NO123456789MVA",
            Industry = "Electronics",
            MerchantCategory = "Retail",
            CommissionRate = 0.035m,
            IsActive = true,
            IsVerified = true,
            OnboardedAt = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var testMerchant2 = new Merchant
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Fashion Boutique",
            Email = "merchant@fashionboutique.no",
            PhoneNumber = "+4787654321",
            BusinessRegistrationNumber = "NO987654321",
            VATNumber = "NO987654321MVA",
            Industry = "Fashion",
            MerchantCategory = "Retail",
            CommissionRate = 0.040m,
            IsActive = true,
            IsVerified = true,
            OnboardedAt = DateTime.UtcNow.AddDays(-60),
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        };

        modelBuilder.Entity<Merchant>().HasData(testMerchant1, testMerchant2);

        // Seed merchant addresses
        modelBuilder.Entity<MerchantAddress>().HasData(
            new MerchantAddress
            {
                Id = Guid.NewGuid(),
                MerchantId = testMerchant1.Id,
                AddressLine1 = "Storgata 1",
                City = "Oslo",
                PostalCode = "0155",
                CountryCode = "NO",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new MerchantAddress
            {
                Id = Guid.NewGuid(),
                MerchantId = testMerchant2.Id,
                AddressLine1 = "Karl Johans gate 10",
                City = "Oslo",
                PostalCode = "0154",
                CountryCode = "NO",
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            }
        );

        // Seed test customers
        var testCustomer1 = new Customer
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            FirstName = "Ola",
            LastName = "Nordmann",
            Email = "ola.nordmann@example.no",
            PhoneNumber = "+4798765432",
            DateOfBirth = new DateTime(1990, 5, 15),
            SocialSecurityNumber = "15059012345",
            RiskLevel = RiskLevel.Low,
            CreditRating = CreditRating.Good,
            CreditScore = 720,
            CreditLimit = 50000,
            AvailableCredit = 50000,
            CollectionStatus = CollectionStatus.Current,
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };

        var testCustomer2 = new Customer
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            FirstName = "Kari",
            LastName = "Hansen",
            Email = "kari.hansen@example.no",
            PhoneNumber = "+4712348765",
            DateOfBirth = new DateTime(1985, 8, 22),
            SocialSecurityNumber = "22088512345",
            RiskLevel = RiskLevel.Medium,
            CreditRating = CreditRating.Fair,
            CreditScore = 680,
            CreditLimit = 30000,
            AvailableCredit = 30000,
            CollectionStatus = CollectionStatus.Current,
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        };

        modelBuilder.Entity<Customer>().HasData(testCustomer1, testCustomer2);

        // Seed customer addresses
        modelBuilder.Entity<CustomerAddress>().HasData(
            new CustomerAddress
            {
                Id = Guid.NewGuid(),
                CustomerId = testCustomer1.Id,
                AddressLine1 = "Bygdøy Allé 5",
                City = "Oslo",
                PostalCode = "0257",
                CountryCode = "NO",
                IsPrimary = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new CustomerAddress
            {
                Id = Guid.NewGuid(),
                CustomerId = testCustomer2.Id,
                AddressLine1 = "Grünerløkka 12",
                City = "Oslo",
                PostalCode = "0554",
                CountryCode = "NO",
                IsPrimary = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            }
        );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is YourCompanyBNPL.Common.Models.BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (YourCompanyBNPL.Common.Models.BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}