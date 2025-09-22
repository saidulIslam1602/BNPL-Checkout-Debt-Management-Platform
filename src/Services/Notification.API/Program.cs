using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Serilog;
using RivertyBNPL.Services.Notification.API.Data;
using RivertyBNPL.Services.Notification.API.Services;
using RivertyBNPL.Services.Notification.API.Providers;
using RivertyBNPL.Services.Notification.API.Validators;
using RivertyBNPL.Services.Notification.API.Mappings;

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
        Title = "Riverty BNPL Notification API", 
        Version = "v1",
        Description = "API for managing notifications in the Riverty BNPL platform"
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
builder.Services.AddAutoMapper(typeof(NotificationMappingProfile));

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

// Configure notification provider options
builder.Services.Configure<NotificationProviderOptions>(
    builder.Configuration.GetSection("NotificationProviders"));

// Register notification services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
builder.Services.AddScoped<INotificationQueueService, NotificationQueueService>();

// Register notification providers
builder.Services.AddScoped<EmailProvider>();
builder.Services.AddScoped<SmsProvider>();
builder.Services.AddScoped<PushProvider>();
builder.Services.AddScoped<INotificationProviderFactory, NotificationProviderFactory>();

// Hangfire for background jobs
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<NotificationQueueProcessor>();

// Register health check services
builder.Services.AddScoped<RivertyBNPL.Services.Notification.API.HealthChecks.NotificationHealthCheck>();
builder.Services.AddScoped<RivertyBNPL.Services.Notification.API.HealthChecks.NotificationProviderHealthCheck>();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContext<NotificationDbContext>()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty)
    .AddCheck<RivertyBNPL.Services.Notification.API.HealthChecks.NotificationHealthCheck>("notification_service")
    .AddCheck<RivertyBNPL.Services.Notification.API.HealthChecks.NotificationProviderHealthCheck>("notification_providers");

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

// Start background job for processing queued notifications
RecurringJob.AddOrUpdate<NotificationQueueService>(
    "process-queued-notifications",
    service => service.ProcessQueuedNotificationsAsync(CancellationToken.None),
    "*/30 * * * * *"); // Every 30 seconds

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