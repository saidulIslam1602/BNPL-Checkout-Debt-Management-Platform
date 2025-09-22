using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Serilog;
using RivertyBNPL.NotificationScheduler.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        services.AddSerilog();

        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Database Context (using Payment DB for now, could be separate)
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // HTTP Client
        services.AddHttpClient();

        // Application Services
        services.AddScoped<IPaymentReminderService, PaymentReminderService>();
        services.AddScoped<IOverdueNotificationService, OverdueNotificationService>();
        services.AddScoped<INotificationQueueService, NotificationQueueService>();
        services.AddScoped<ICustomerCommunicationService, CustomerCommunicationService>();

        // External service clients
        services.AddHttpClient<INotificationApiClient, NotificationApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:NotificationApi:BaseUrl"]!);
            client.DefaultRequestHeaders.Add("ApiKey", configuration["Services:NotificationApi:ApiKey"]);
        });

        services.AddHttpClient<IPaymentApiClient, PaymentApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:PaymentApi:BaseUrl"]!);
            client.DefaultRequestHeaders.Add("ApiKey", configuration["Services:PaymentApi:ApiKey"]);
        });
    })
    .UseSerilog()
    .Build();

host.Run();