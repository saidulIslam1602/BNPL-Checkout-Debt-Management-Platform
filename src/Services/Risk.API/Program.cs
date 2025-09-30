using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Reflection;
using Serilog;
using YourCompanyBNPL.Risk.API.Data;
using YourCompanyBNPL.Risk.API.Services;
using YourCompanyBNPL.Risk.API.Mappings;
using YourCompanyBNPL.Risk.API.Infrastructure;
using YourCompanyBNPL.Shared.Infrastructure.Security;
using YourCompanyBNPL.Shared.Infrastructure.ServiceBus;
using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Risk.API")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

// Database
builder.Services.AddDbContext<RiskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("YourCompanyBNPL.Risk.API")));

// AutoMapper
builder.Services.AddAutoMapper(typeof(RiskMappingProfile));

// Application Services
builder.Services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddScoped<ICreditBureauService, CreditBureauService>();
builder.Services.AddScoped<IMachineLearningService, MachineLearningService>();

// HTTP Client for external services
builder.Services.AddHttpClient<ICreditBureauService, CreditBureauService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "YourCompanyBNPL-Risk-API/1.0");
});

// Configure Credit Bureau options
builder.Services.Configure<CreditBureauOptions>(
    builder.Configuration.GetSection("CreditBureau"));

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RiskAssessment", policy =>
        policy.RequireClaim("scope", "risk.assessment"));
    options.AddPolicy("FraudDetection", policy =>
        policy.RequireClaim("scope", "fraud.detection"));
    options.AddPolicy("ModelManagement", policy =>
        policy.RequireClaim("scope", "model.management"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<RiskDbContext>("database")
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not configured"),
        name: "sql-server")
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck("credit-bureau", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck<MLModelHealthCheck>("ml-models");

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "YourCompany BNPL Risk Assessment API",
        Version = "v1",
        Description = "Comprehensive risk assessment and fraud detection API for BNPL services",
        Contact = new OpenApiContact
        {
            Name = "YourCompany Development Team",
            Email = "dev@yourcompany.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRateLimitingService, RateLimitingService>();

// Circuit Breaker
builder.Services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();

// Service Bus (for events)
builder.Services.AddScoped<IServiceBusService, ServiceBusService>();

// Background Services
builder.Services.AddHostedService<ModelRetrainingService>();
builder.Services.AddHostedService<FraudRuleUpdateService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Risk Assessment API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowedOrigins");

// Security middleware
app.UseMiddleware<SecurityMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Rate limiting middleware
app.UseMiddleware<RateLimitingMiddleware>();

// Health checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapControllers();

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RiskDbContext>();
    
    if (app.Environment.IsDevelopment())
    {
        await context.Database.EnsureCreatedAsync();
    }
    else
    {
        await context.Database.MigrateAsync();
    }
}

try
{
    Log.Information("Starting Risk Assessment API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Risk Assessment API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Health check for ML models
/// </summary>
public class MLModelHealthCheck : IHealthCheck
{
    private readonly IMachineLearningService _mlService;
    private readonly ILogger<MLModelHealthCheck> _logger;

    public MLModelHealthCheck(IMachineLearningService mlService, ILogger<MLModelHealthCheck> logger)
    {
        _mlService = mlService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if ML models are available and responding
            var testFeatures = new Dictionary<string, object>
            {
                { "CreditScore", 700 },
                { "AnnualIncome", 500000 },
                { "ExistingDebt", 100000 },
                { "PaymentHistoryMonths", 24 },
                { "LatePaymentsLast12Months", 0 },
                { "ExistingCreditAccounts", 3 },
                { "RequestedAmount", 10000 },
                { "Age", 30 },
                { "EmploymentLength", 5 },
                { "ResidentialStability", 3 },
                { "DebtToIncomeRatio", 0.2m },
                { "HasBankruptcy", false },
                { "HasCollections", false }
            };

            var result = await _mlService.PredictCreditRiskAsync(testFeatures, cancellationToken);
            
            if (result.Success)
            {
                return HealthCheckResult.Healthy("ML models are responding correctly");
            }
            else
            {
                return HealthCheckResult.Degraded($"ML models responding with errors: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ML model health check failed");
            return HealthCheckResult.Unhealthy("ML models are not responding", ex);
        }
    }
}

/// <summary>
/// Background service for model retraining
/// </summary>
public class ModelRetrainingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModelRetrainingService> _logger;
    private readonly TimeSpan _retrainingInterval = TimeSpan.FromDays(7); // Weekly retraining

    public ModelRetrainingService(IServiceProvider serviceProvider, ILogger<ModelRetrainingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled model retraining");

                using var scope = _serviceProvider.CreateScope();
                var mlService = scope.ServiceProvider.GetRequiredService<IMachineLearningService>();
                
                var result = await mlService.RetrainModelsAsync(stoppingToken);
                
                if (result.Success)
                {
                    _logger.LogInformation("Scheduled model retraining completed successfully");
                }
                else
                {
                    _logger.LogError("Scheduled model retraining failed: {Error}", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled model retraining");
            }

            await Task.Delay(_retrainingInterval, stoppingToken);
        }
    }
}

/// <summary>
/// Background service for fraud rule updates
/// </summary>
public class FraudRuleUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FraudRuleUpdateService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(6); // Every 6 hours

    public FraudRuleUpdateService(IServiceProvider serviceProvider, ILogger<FraudRuleUpdateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled fraud rule update");

                using var scope = _serviceProvider.CreateScope();
                var fraudService = scope.ServiceProvider.GetRequiredService<IFraudDetectionService>();
                
                var result = await fraudService.UpdateFraudRulesAsync(stoppingToken);
                
                if (result.Success)
                {
                    _logger.LogInformation("Scheduled fraud rule update completed successfully");
                }
                else
                {
                    _logger.LogError("Scheduled fraud rule update failed: {Error}", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled fraud rule update");
            }

            await Task.Delay(_updateInterval, stoppingToken);
        }
    }
}