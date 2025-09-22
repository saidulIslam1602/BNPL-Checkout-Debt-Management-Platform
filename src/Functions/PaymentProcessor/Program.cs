using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Serilog;
using Serilog.Events;

using RivertyBNPL.Functions.PaymentProcessor.Services;
using RivertyBNPL.Functions.PaymentProcessor.Data;
using RivertyBNPL.Functions.PaymentProcessor.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        var env = context.HostingEnvironment.EnvironmentName;
        
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();

        // Add Azure Key Vault in production
        if (env == "Production")
        {
            var keyVaultUri = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URI");
            if (!string.IsNullOrEmpty(keyVaultUri))
            {
                config.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
            }
        }
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Database Context
        services.AddDbContext<PaymentProcessorDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // HTTP Clients with Polly retry policies
        services.AddHttpClient<IPaymentGatewayService, PaymentGatewayService>()
                .AddPolicyHandler(RetryPolicies.GetRetryPolicy())
                .AddPolicyHandler(RetryPolicies.GetCircuitBreakerPolicy());

        services.AddHttpClient<IRiskAssessmentService, RiskAssessmentService>()
                .AddPolicyHandler(RetryPolicies.GetRetryPolicy())
                .AddPolicyHandler(RetryPolicies.GetCircuitBreakerPolicy());

        services.AddHttpClient<INotificationService, NotificationService>()
                .AddPolicyHandler(RetryPolicies.GetRetryPolicy())
                .AddPolicyHandler(RetryPolicies.GetCircuitBreakerPolicy());

        services.AddHttpClient<ISettlementService, SettlementService>()
                .AddPolicyHandler(RetryPolicies.GetRetryPolicy())
                .AddPolicyHandler(RetryPolicies.GetCircuitBreakerPolicy());

        // Business Services
        services.AddScoped<IInstallmentProcessorService, InstallmentProcessorService>();
        services.AddScoped<IPaymentReminderService, PaymentReminderService>();
        services.AddScoped<ISettlementProcessorService, SettlementProcessorService>();
        services.AddScoped<IRiskMonitoringService, RiskMonitoringService>();
        services.AddScoped<IReportingService, ReportingService>();

        // Configuration
        services.Configure<PaymentProcessorOptions>(configuration.GetSection("PaymentProcessor"));
        services.Configure<NotificationOptions>(configuration.GetSection("Notifications"));
        services.Configure<SettlementOptions>(configuration.GetSection("Settlements"));

        // Azure Service Bus
        services.AddSingleton<IServiceBusService, ServiceBusService>();

        // Logging with Serilog
        services.AddSerilog((serviceProvider, loggerConfig) =>
        {
            var appInsightsConnectionString = configuration.GetValue<string>("ApplicationInsights:ConnectionString");
            
            loggerConfig
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "PaymentProcessor")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.ApplicationInsights(appInsightsConnectionString, TelemetryConverter.Traces);
        });
    })
    .Build();

// Initialize database
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentProcessorDbContext>();
    await context.Database.MigrateAsync();
}

host.Run();