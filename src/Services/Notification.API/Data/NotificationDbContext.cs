using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using YourCompanyBNPL.Notification.API.Models;
using System.Text.Json;

namespace YourCompanyBNPL.Notification.API.Data;

/// <summary>
/// Database context for notification service
/// </summary>
public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Notification> Notifications { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<NotificationDelivery> NotificationDeliveries { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<WebhookConfig> WebhookConfigs { get; set; }
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Notification entity
        modelBuilder.Entity<Models.Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.InstallmentId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.BatchId);
            entity.HasIndex(e => e.CampaignId);

            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Channel).IsRequired();
            entity.Property(e => e.Recipient).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.TemplateData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Tags).HasColumnType("nvarchar(max)");
            entity.Property(e => e.BatchId).HasMaxLength(100);
            entity.Property(e => e.CampaignId).HasMaxLength(100);
        });

        // Configure NotificationTemplate entity
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.Language);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.Name, e.Language }).IsUnique();

            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Channel).IsRequired();
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.HtmlContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TextContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.SmsContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PushContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Language).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Variables).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
        });

        // Configure NotificationPreference entity
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.CustomerId).IsUnique();

            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Preferences).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TimeZone).HasMaxLength(50);
        });

        // Configure NotificationDelivery entity
        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.NotificationId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DeliveredAt);

            entity.Property(e => e.NotificationId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ErrorMessage).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Response).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ExternalId).HasMaxLength(100);
        });

        // Configure Campaign entity
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Channel).IsRequired();
            entity.Property(e => e.TemplateId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.TargetCriteria).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Settings).HasColumnType("nvarchar(max)");

            entity.HasOne<NotificationTemplate>()
                  .WithMany()
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed default templates
        SeedDefaultTemplates(modelBuilder);
    }

    private static void SeedDefaultTemplates(ModelBuilder modelBuilder)
    {
        var templates = new List<NotificationTemplate>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "payment_reminder",
                DisplayName = "Payment Reminder",
                Description = "Reminds customers about upcoming payments",
                Type = "PaymentReminder",
                Channel = NotificationChannel.Email,
                Subject = "Påminnelse: Betaling forfaller snart",
                HtmlContent = "<h2>Påminnelse: Betaling forfaller snart</h2><p>Hei {{CustomerName}},</p><p>Din betaling på <strong>{{Amount}} NOK</strong> forfaller <strong>{{DueDate}}</strong>.</p><p>Vennlig hilsen,<br>YourCompany BNPL</p>",
                TextContent = "Hei {{CustomerName}},\n\nDin betaling på {{Amount}} NOK forfaller {{DueDate}}.\n\nVennlig hilsen,\nYourCompany BNPL",
                Language = "nb-NO",
                IsActive = true,
                Version = 1,
                Variables = "[\"CustomerName\", \"Amount\", \"DueDate\"]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "payment_confirmation",
                DisplayName = "Payment Confirmation",
                Description = "Confirms successful payment",
                Type = "PaymentConfirmation",
                Channel = NotificationChannel.Email,
                Subject = "Betalingsbekreftelse",
                HtmlContent = "<h2>Betalingsbekreftelse</h2><p>Hei {{CustomerName}},</p><p>Din betaling på <strong>{{Amount}} NOK</strong> er mottatt.</p><p>Vennlig hilsen,<br>YourCompany BNPL</p>",
                TextContent = "Hei {{CustomerName}},\n\nDin betaling på {{Amount}} NOK er mottatt.\n\nVennlig hilsen,\nYourCompany BNPL",
                Language = "nb-NO",
                IsActive = true,
                Version = 1,
                Variables = "[\"CustomerName\", \"Amount\"]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "payment_overdue",
                DisplayName = "Payment Overdue",
                Description = "Notifies customers about overdue payments",
                Type = "PaymentOverdue",
                Channel = NotificationChannel.Email,
                Subject = "Forfalt betaling - Handling kreves",
                HtmlContent = "<h2>Forfalt betaling - Handling kreves</h2><p>Hei {{CustomerName}},</p><p>Din betaling på <strong>{{Amount}} NOK</strong> forfalt {{DueDate}}. Vennligst betal så snart som mulig.</p><p>Vennlig hilsen,<br>YourCompany BNPL</p>",
                TextContent = "Hei {{CustomerName}},\n\nDin betaling på {{Amount}} NOK forfalt {{DueDate}}. Vennligst betal så snart som mulig.\n\nVennlig hilsen,\nYourCompany BNPL",
                Language = "nb-NO",
                IsActive = true,
                Version = 1,
                Variables = "[\"CustomerName\", \"Amount\", \"DueDate\"]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        modelBuilder.Entity<NotificationTemplate>().HasData(templates);

        // Configure WebhookConfig entity
        modelBuilder.Entity<WebhookConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Secret).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Events).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Headers).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Configure WebhookDelivery entity
        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.WebhookConfigId);
            entity.HasIndex(e => e.NotificationId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NextRetryAt);

            entity.Property(e => e.WebhookConfigId).IsRequired();
            entity.Property(e => e.NotificationId).IsRequired();
            entity.Property(e => e.Event).IsRequired();
            entity.Property(e => e.Payload).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ResponseBody).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ErrorMessage).HasColumnType("nvarchar(max)");

            // Configure relationships
            entity.HasOne(e => e.WebhookConfig)
                .WithMany()
                .HasForeignKey(e => e.WebhookConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Notification)
                .WithMany()
                .HasForeignKey(e => e.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}