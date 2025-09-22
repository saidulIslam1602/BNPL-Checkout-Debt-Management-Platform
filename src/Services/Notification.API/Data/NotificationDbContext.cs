using Microsoft.EntityFrameworkCore;
using RivertyBNPL.Notification.API.Models;
using System.Text.Json;

namespace RivertyBNPL.Notification.API.Data;

/// <summary>
/// Database context for notification service
/// </summary>
public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<NotificationDelivery> NotificationDeliveries { get; set; }
    public DbSet<NotificationCampaign> NotificationCampaigns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
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
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.CampaignId);
            entity.HasIndex(e => e.BatchId);

            entity.Property(e => e.Content).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TemplateData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ExternalResponse).HasColumnType("nvarchar(max)");
        });

        // Configure NotificationTemplate entity
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.Language);
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.HtmlContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TextContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.SmsContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PushContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Variables).HasColumnType("nvarchar(max)");
        });

        // Configure NotificationPreference entity
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => new { e.CustomerId, e.NotificationType, e.Channel }).IsUnique();

            entity.Property(e => e.Settings).HasColumnType("nvarchar(max)");
        });

        // Configure NotificationDelivery entity
        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.HasIndex(e => e.NotificationId);
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AttemptedAt);
            entity.HasIndex(e => e.DeliveredAt);

            entity.HasOne(e => e.Notification)
                  .WithMany()
                  .HasForeignKey(e => e.NotificationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ExternalResponse).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
        });

        // Configure NotificationCampaign entity
        modelBuilder.Entity<NotificationCampaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Template)
                  .WithMany()
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.TargetCriteria).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Settings).HasColumnType("nvarchar(max)");
        });

        // Seed default notification templates
        SeedDefaultTemplates(modelBuilder);
    }

    private static void SeedDefaultTemplates(ModelBuilder modelBuilder)
    {
        var templates = new List<NotificationTemplate>
        {
            // Payment reminder templates
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "payment_reminder_email_no",
                Type = "payment_reminder",
                Channel = NotificationChannel.Email,
                Language = "no",
                Subject = "Påminnelse: Betaling forfaller snart",
                HtmlContent = @"
                    <html>
                    <body>
                        <h2>Hei {{customer_name}},</h2>
                        <p>Dette er en påminnelse om at din betaling forfaller snart.</p>
                        <p><strong>Betalingsdetaljer:</strong></p>
                        <ul>
                            <li>Beløp: {{amount}} {{currency}}</li>
                            <li>Forfallsdato: {{due_date}}</li>
                            <li>Betalingsreferanse: {{payment_reference}}</li>
                        </ul>
                        <p>Vennligst betal innen forfallsdatoen for å unngå forsinkelsesgebyrer.</p>
                        <p><a href='{{payment_link}}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Betal nå</a></p>
                        <p>Med vennlig hilsen,<br>Riverty</p>
                    </body>
                    </html>",
                TextContent = "Hei {{customer_name}}, Dette er en påminnelse om at din betaling på {{amount}} {{currency}} forfaller {{due_date}}. Betalingsreferanse: {{payment_reference}}. Betal på: {{payment_link}}",
                SmsContent = "Riverty: Din betaling på {{amount}} {{currency}} forfaller {{due_date}}. Betal på: {{payment_link}}",
                IsActive = true,
                Variables = JsonSerializer.Serialize(new[] { "customer_name", "amount", "currency", "due_date", "payment_reference", "payment_link" }),
                Description = "Norwegian payment reminder template",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "payment_reminder_email_en",
                Type = "payment_reminder",
                Channel = NotificationChannel.Email,
                Language = "en",
                Subject = "Reminder: Payment due soon",
                HtmlContent = @"
                    <html>
                    <body>
                        <h2>Hi {{customer_name}},</h2>
                        <p>This is a reminder that your payment is due soon.</p>
                        <p><strong>Payment Details:</strong></p>
                        <ul>
                            <li>Amount: {{amount}} {{currency}}</li>
                            <li>Due Date: {{due_date}}</li>
                            <li>Payment Reference: {{payment_reference}}</li>
                        </ul>
                        <p>Please pay before the due date to avoid late fees.</p>
                        <p><a href='{{payment_link}}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Pay Now</a></p>
                        <p>Best regards,<br>Riverty</p>
                    </body>
                    </html>",
                TextContent = "Hi {{customer_name}}, This is a reminder that your payment of {{amount}} {{currency}} is due {{due_date}}. Payment reference: {{payment_reference}}. Pay at: {{payment_link}}",
                SmsContent = "Riverty: Your payment of {{amount}} {{currency}} is due {{due_date}}. Pay at: {{payment_link}}",
                IsActive = true,
                Variables = JsonSerializer.Serialize(new[] { "customer_name", "amount", "currency", "due_date", "payment_reference", "payment_link" }),
                Description = "English payment reminder template",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            },
            // Overdue payment templates
            new NotificationTemplate
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "overdue_payment_email_no",
                Type = "overdue_payment",
                Channel = NotificationChannel.Email,
                Language = "no",
                Subject = "VIKTIG: Forsinket betaling - Handling kreves",
                HtmlContent = @"
                    <html>
                    <body>
                        <h2>Hei {{customer_name}},</h2>
                        <p><strong>Din betaling er forsinket og krever umiddelbar oppmerksomhet.</strong></p>
                        <p><strong>Betalingsdetaljer:</strong></p>
                        <ul>
                            <li>Beløp: {{amount}} {{currency}}</li>
                            <li>Opprinnelig forfallsdato: {{original_due_date}}</li>
                            <li>Dager forsinket: {{days_overdue}}</li>
                            <li>Forsinkelsesgebyr: {{late_fee}} {{currency}}</li>
                            <li>Totalt å betale: {{total_amount}} {{currency}}</li>
                        </ul>
                        <p>Vennligst betal umiddelbart for å unngå ytterligere gebyrer og mulige inkassohandlinger.</p>
                        <p><a href='{{payment_link}}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Betal nå</a></p>
                        <p>Hvis du har spørsmål, kontakt oss på kundeservice@riverty.no</p>
                        <p>Med vennlig hilsen,<br>Riverty</p>
                    </body>
                    </html>",
                TextContent = "VIKTIG: Din betaling på {{total_amount}} {{currency}} er {{days_overdue}} dager forsinket. Betal umiddelbart på: {{payment_link}}",
                SmsContent = "Riverty VIKTIG: Betaling {{total_amount}} {{currency}} er {{days_overdue}} dager forsinket. Betal: {{payment_link}}",
                IsActive = true,
                Variables = JsonSerializer.Serialize(new[] { "customer_name", "amount", "currency", "original_due_date", "days_overdue", "late_fee", "total_amount", "payment_link" }),
                Description = "Norwegian overdue payment template",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            },
            // Payment confirmation templates
            new NotificationTemplate
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "payment_confirmation_email_no",
                Type = "payment_confirmation",
                Channel = NotificationChannel.Email,
                Language = "no",
                Subject = "Betalingsbekreftelse - Takk for din betaling",
                HtmlContent = @"
                    <html>
                    <body>
                        <h2>Hei {{customer_name}},</h2>
                        <p>Takk for din betaling! Vi har mottatt følgende betaling:</p>
                        <p><strong>Betalingsdetaljer:</strong></p>
                        <ul>
                            <li>Beløp: {{amount}} {{currency}}</li>
                            <li>Betalingsdato: {{payment_date}}</li>
                            <li>Betalingsreferanse: {{payment_reference}}</li>
                            <li>Transaksjons-ID: {{transaction_id}}</li>
                        </ul>
                        <p>Din betaling er nå behandlet og kontoen din er oppdatert.</p>
                        <p>Du kan se alle dine betalinger i <a href='{{customer_portal_link}}'>kundeportalen</a>.</p>
                        <p>Med vennlig hilsen,<br>Riverty</p>
                    </body>
                    </html>",
                TextContent = "Takk for din betaling på {{amount}} {{currency}}! Betalingsreferanse: {{payment_reference}}. Se detaljer i kundeportalen: {{customer_portal_link}}",
                SmsContent = "Riverty: Betaling på {{amount}} {{currency}} mottatt. Ref: {{payment_reference}}",
                IsActive = true,
                Variables = JsonSerializer.Serialize(new[] { "customer_name", "amount", "currency", "payment_date", "payment_reference", "transaction_id", "customer_portal_link" }),
                Description = "Norwegian payment confirmation template",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            },
            // BNPL approval templates
            new NotificationTemplate
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "bnpl_approval_email_no",
                Type = "bnpl_approval",
                Channel = NotificationChannel.Email,
                Language = "no",
                Subject = "BNPL-plan godkjent - Dine betalingsdetaljer",
                HtmlContent = @"
                    <html>
                    <body>
                        <h2>Gratulerer {{customer_name}}!</h2>
                        <p>Din BNPL-plan har blitt godkjent. Her er detaljene:</p>
                        <p><strong>Kjøpsdetaljer:</strong></p>
                        <ul>
                            <li>Kjøpsbeløp: {{purchase_amount}} {{currency}}</li>
                            <li>Butikk: {{merchant_name}}</li>
                            <li>Kjøpsdato: {{purchase_date}}</li>
                        </ul>
                        <p><strong>Betalingsplan:</strong></p>
                        <ul>
                            <li>Antall avdrag: {{installment_count}}</li>
                            <li>Avdragsbeløp: {{installment_amount}} {{currency}}</li>
                            <li>Første forfallsdato: {{first_due_date}}</li>
                            <li>Siste forfallsdato: {{last_due_date}}</li>
                        </ul>
                        <p>Du vil motta påminnelser før hver forfallsdato.</p>
                        <p><a href='{{customer_portal_link}}'>Se full betalingsplan</a></p>
                        <p>Med vennlig hilsen,<br>Riverty</p>
                    </body>
                    </html>",
                TextContent = "BNPL-plan godkjent! {{installment_count}} avdrag på {{installment_amount}} {{currency}}. Første forfallsdato: {{first_due_date}}. Se detaljer: {{customer_portal_link}}",
                IsActive = true,
                Variables = JsonSerializer.Serialize(new[] { "customer_name", "purchase_amount", "currency", "merchant_name", "purchase_date", "installment_count", "installment_amount", "first_due_date", "last_due_date", "customer_portal_link" }),
                Description = "Norwegian BNPL approval template",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            }
        };

        modelBuilder.Entity<NotificationTemplate>().HasData(templates);
    }
}