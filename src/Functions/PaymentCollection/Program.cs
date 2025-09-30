using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YourCompanyBNPL.PaymentCollection.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddApplicationInsights();
        });
        
        // Register services
        services.AddScoped<IAutomaticCollectionService, AutomaticCollectionService>();
        services.AddScoped<IOverdueProcessingService, OverdueProcessingService>();
        services.AddScoped<ISettlementProcessingService, SettlementProcessingService>();
        
        // Add HTTP client
        services.AddHttpClient();
    })
    .Build();

host.Run();