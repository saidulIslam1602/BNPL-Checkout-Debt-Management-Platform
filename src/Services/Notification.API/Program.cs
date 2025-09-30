using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Serilog;
using AutoMapper;
using YourCompanyBNPL.Notification.API.Data;
using YourCompanyBNPL.Notification.API.Services;
using YourCompanyBNPL.Notification.API.Validators;
using YourCompanyBNPL.Notification.API.Middleware;
using YourCompanyBNPL.Notification.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/notification-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "YourCompany BNPL Notification API", 
        Version = "v1",
        Description = "API for managing notifications in the YourCompany BNPL platform"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
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
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database configuration
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<SendNotificationRequestValidator>();

// JWT Authentication
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

builder.Services.AddAuthorization();

// Configure service settings
// Configure notification settings - these classes need to be implemented
// builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
// builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("Sms"));
// builder.Services.Configure<PushSettings>(builder.Configuration.GetSection("Push"));

// Add caching
builder.Services.AddCaching(builder.Configuration);

// Add rate limiting and throttling
builder.Services.AddRateLimiting(builder.Configuration);
// builder.Services.AddNotificationThrottling(builder.Configuration); // Extension method needs to be implemented

// Add HTTP client for webhooks
builder.Services.AddHttpClient();

// Register infrastructure services
// builder.Services.AddCircuitBreaker(options =>
// {
//     options.RetryCount = 3;
//     options.FailureThreshold = 5;
//     options.BreakDuration = TimeSpan.FromMinutes(1);
// }); // Extension method needs to be implemented

// Register advanced template engine
builder.Services.AddSingleton<IAdvancedTemplateEngine, AdvancedTemplateEngine>();
builder.Services.AddSingleton<TemplateEngineOptions>();

// Register core services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
// builder.Services.AddScoped<ICampaignService, CampaignService>(); // CampaignService needs to be implemented
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<INotificationScheduler, NotificationScheduler>();

// Register background services
builder.Services.AddHostedService<ScheduledNotificationProcessor>();

// Register services with caching decorators
builder.Services.AddScoped<TemplateService>();
// Template service registration - CachedTemplateService needs to be implemented
builder.Services.AddScoped<ITemplateService, TemplateService>();

builder.Services.AddScoped<PreferenceService>();
// Preference service registration - CachedPreferenceService needs to be implemented
builder.Services.AddScoped<IPreferenceService, PreferenceService>();

// Hangfire for background jobs
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Application Insights
if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add global exception handling
app.UseExceptionHandling();

// Add rate limiting
app.UseRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Hangfire dashboard
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Log.Information("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize database");
    }
}

// Start background jobs for processing notifications
RecurringJob.AddOrUpdate<INotificationService>(
    "process-scheduled-notifications",
    service => service.ProcessScheduledNotificationsAsync(CancellationToken.None),
    "*/5 * * * *"); // Every 5 minutes

RecurringJob.AddOrUpdate<INotificationService>(
    "process-retry-notifications",
    service => service.ProcessRetryNotificationsAsync(CancellationToken.None),
    "*/10 * * * *"); // Every 10 minutes

Log.Information("Notification API starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}