using Microsoft.EntityFrameworkCore;
using RivertyBNPL.Risk.API.Models;
using RivertyBNPL.Common.Enums;
using System.Text.Json;

namespace RivertyBNPL.Risk.API.Data;

/// <summary>
/// Database context for the Risk Assessment service
/// </summary>
public class RiskDbContext : DbContext
{
    public RiskDbContext(DbContextOptions<RiskDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<CreditAssessment> CreditAssessments { get; set; }
    public DbSet<RiskFactor> RiskFactors { get; set; }
    public DbSet<CreditBureauResponse> CreditBureauResponses { get; set; }
    public DbSet<FraudDetection> FraudDetections { get; set; }
    public DbSet<FraudRule> FraudRules { get; set; }
    public DbSet<CustomerRiskProfile> CustomerRiskProfiles { get; set; }
    public DbSet<RiskRule> RiskRules { get; set; }
    public DbSet<RiskModel> RiskModels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure CreditAssessment entity
        modelBuilder.Entity<CreditAssessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.SocialSecurityNumber);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.AssessmentDate);
            entity.HasIndex(e => e.CreditScore);
            entity.HasIndex(e => e.RiskLevel);
            entity.HasIndex(e => e.IsApproved);
            
            entity.Property(e => e.Currency)
                .HasConversion<int>();
            
            entity.Property(e => e.PlanType)
                .HasConversion<int>();
            
            entity.Property(e => e.CreditRating)
                .HasConversion<int>();
            
            entity.Property(e => e.RiskLevel)
                .HasConversion<int>();

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
        });

        // Configure RiskFactor entity
        modelBuilder.Entity<RiskFactor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreditAssessmentId);
            entity.HasIndex(e => e.FactorType);
            entity.HasIndex(e => e.Impact);
            
            entity.Property(e => e.Impact)
                .HasConversion<int>();

            entity.HasOne(e => e.CreditAssessment)
                .WithMany(ca => ca.RiskFactors)
                .HasForeignKey(e => e.CreditAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CreditBureauResponse entity
        modelBuilder.Entity<CreditBureauResponse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreditAssessmentId);
            entity.HasIndex(e => e.BureauName);
            entity.HasIndex(e => e.RequestId);
            entity.HasIndex(e => e.ResponseDate);

            entity.HasOne(e => e.CreditAssessment)
                .WithMany(ca => ca.CreditBureauResponses)
                .HasForeignKey(e => e.CreditAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FraudDetection entity
        modelBuilder.Entity<FraudDetection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.FraudRiskLevel);
            entity.HasIndex(e => e.FraudScore);
            entity.HasIndex(e => e.IsBlocked);
            entity.HasIndex(e => e.DetectionDate);
            
            entity.Property(e => e.Currency)
                .HasConversion<int>();
            
            entity.Property(e => e.FraudRiskLevel)
                .HasConversion<int>();

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
        });

        // Configure FraudRule entity
        modelBuilder.Entity<FraudRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FraudDetectionId);
            entity.HasIndex(e => e.RuleName);
            entity.HasIndex(e => e.Severity);
            
            entity.Property(e => e.Severity)
                .HasConversion<int>();

            entity.HasOne(e => e.FraudDetection)
                .WithMany(fd => fd.TriggeredRules)
                .HasForeignKey(e => e.FraudDetectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CustomerRiskProfile entity
        modelBuilder.Entity<CustomerRiskProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId).IsUnique();
            entity.HasIndex(e => e.CurrentRiskLevel);
            entity.HasIndex(e => e.CurrentCreditScore);
            entity.HasIndex(e => e.CurrentCreditRating);
            entity.HasIndex(e => e.IsBlacklisted);
            entity.HasIndex(e => e.LastAssessmentDate);
            entity.HasIndex(e => e.NextReviewDate);
            
            entity.Property(e => e.CurrentRiskLevel)
                .HasConversion<int>();
            
            entity.Property(e => e.CurrentCreditRating)
                .HasConversion<int>();

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
        });

        // Configure RiskRule entity
        modelBuilder.Entity<RiskRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RuleName).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.EffectiveFrom);
            entity.HasIndex(e => e.EffectiveTo);
        });

        // Configure RiskModel entity
        modelBuilder.Entity<RiskModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ModelName);
            entity.HasIndex(e => e.Version);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DeployedDate);
            entity.HasIndex(e => new { e.ModelName, e.Version }).IsUnique();
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed risk rules
        var riskRules = new[]
        {
            new RiskRule
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                RuleName = "MinimumCreditScore",
                Description = "Minimum credit score requirement for BNPL approval",
                Category = "CreditScore",
                IsActive = true,
                Priority = 1,
                Weight = 0.3m,
                MinScore = 600,
                MaxScore = 850,
                Conditions = "CreditScore >= 600",
                Action = "Approve if score >= 600, otherwise decline",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new RiskRule
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RuleName = "MaximumDebtToIncomeRatio",
                Description = "Maximum debt-to-income ratio for BNPL approval",
                Category = "DebtToIncome",
                IsActive = true,
                Priority = 2,
                Weight = 0.25m,
                MinScore = 0,
                MaxScore = 100,
                Conditions = "(ExistingDebt + RequestedAmount) / AnnualIncome <= 0.4",
                Action = "Approve if DTI <= 40%, otherwise decline",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new RiskRule
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                RuleName = "RecentBankruptcy",
                Description = "Check for recent bankruptcy filings",
                Category = "Bankruptcy",
                IsActive = true,
                Priority = 3,
                Weight = 0.4m,
                MinScore = 0,
                MaxScore = 100,
                Conditions = "HasBankruptcy == false",
                Action = "Decline if bankruptcy in last 7 years",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new RiskRule
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                RuleName = "MaximumActiveBNPLPlans",
                Description = "Maximum number of active BNPL plans per customer",
                Category = "BNPLLimits",
                IsActive = true,
                Priority = 4,
                Weight = 0.15m,
                MinScore = 0,
                MaxScore = 5,
                Conditions = "ActiveBNPLPlans <= 5",
                Action = "Decline if more than 5 active plans",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new RiskRule
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                RuleName = "RecentLatePayments",
                Description = "Check for recent late payments",
                Category = "PaymentHistory",
                IsActive = true,
                Priority = 5,
                Weight = 0.2m,
                MinScore = 0,
                MaxScore = 12,
                Conditions = "LatePaymentsLast12Months <= 2",
                Action = "Decline if more than 2 late payments in last 12 months",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            }
        };

        modelBuilder.Entity<RiskRule>().HasData(riskRules);

        // Seed risk model
        var riskModel = new RiskModel
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            ModelName = "BNPL_Risk_Scorer_v1",
            Version = "1.0.0",
            Description = "Machine learning model for BNPL risk assessment",
            ModelType = "RandomForest",
            IsActive = true,
            TrainedDate = DateTime.UtcNow.AddDays(-7),
            DeployedDate = DateTime.UtcNow.AddDays(-3),
            Accuracy = 0.87m,
            Precision = 0.85m,
            Recall = 0.89m,
            TrainingDataSize = 50000,
            Features = JsonSerializer.Serialize(new[]
            {
                "CreditScore", "AnnualIncome", "ExistingDebt", "PaymentHistoryMonths",
                "LatePaymentsLast12Months", "ExistingCreditAccounts", "RequestedAmount",
                "Age", "EmploymentLength", "ResidentialStability"
            }),
            Hyperparameters = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "n_estimators", 100 },
                { "max_depth", 10 },
                { "min_samples_split", 5 },
                { "min_samples_leaf", 2 }
            }),
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        modelBuilder.Entity<RiskModel>().HasData(riskModel);

        // Seed sample customer risk profiles
        var customerRiskProfiles = new[]
        {
            new CustomerRiskProfile
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                CustomerId = Guid.Parse("33333333-3333-3333-3333-333333333333"), // Ola Nordmann
                CurrentRiskLevel = RiskLevel.Low,
                CurrentCreditScore = 720,
                CurrentCreditRating = CreditRating.Good,
                TotalCreditLimit = 50000,
                AvailableCreditLimit = 50000,
                TotalOutstandingDebt = 0,
                TotalBNPLPlans = 0,
                ActiveBNPLPlans = 0,
                TotalBNPLDebt = 0,
                TotalPayments = 0,
                SuccessfulPayments = 0,
                FailedPayments = 0,
                LatePayments = 0,
                LastPaymentDate = DateTime.UtcNow.AddDays(-30),
                LastAssessmentDate = DateTime.UtcNow.AddDays(-1),
                NextReviewDate = DateTime.UtcNow.AddDays(90),
                IsBlacklisted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new CustomerRiskProfile
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                CustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444"), // Kari Hansen
                CurrentRiskLevel = RiskLevel.Medium,
                CurrentCreditScore = 680,
                CurrentCreditRating = CreditRating.Fair,
                TotalCreditLimit = 30000,
                AvailableCreditLimit = 30000,
                TotalOutstandingDebt = 0,
                TotalBNPLPlans = 0,
                ActiveBNPLPlans = 0,
                TotalBNPLDebt = 0,
                TotalPayments = 0,
                SuccessfulPayments = 0,
                FailedPayments = 0,
                LatePayments = 0,
                LastPaymentDate = DateTime.UtcNow.AddDays(-45),
                LastAssessmentDate = DateTime.UtcNow.AddDays(-2),
                NextReviewDate = DateTime.UtcNow.AddDays(60),
                IsBlacklisted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-45)
            }
        };

        modelBuilder.Entity<CustomerRiskProfile>().HasData(customerRiskProfiles);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is RivertyBNPL.Common.Models.BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (RivertyBNPL.Common.Models.BaseEntity)entry.Entity;
            
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