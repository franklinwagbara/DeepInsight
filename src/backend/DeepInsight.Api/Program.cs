using System.Text;
using DeepInsight.Api.Hubs;
using DeepInsight.Api.Middleware;
using DeepInsight.Core.Interfaces;
using DeepInsight.Infrastructure.Persistence;
using DeepInsight.Infrastructure.Queue;
using DeepInsight.Infrastructure.Services;
using DeepInsight.Processing.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));

// ClickHouse
var clickHouseConn = builder.Configuration.GetConnectionString("ClickHouse") ?? "Host=localhost;Port=8123;Database=deepinsight";
builder.Services.AddSingleton<IEventStore>(new ClickHouseEventStore(clickHouseConn));

// Repositories & Services
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IEventQueuePublisher, RedisStreamPublisher>();
builder.Services.AddSingleton<RedisStreamConsumer>();
builder.Services.AddSingleton<DeviceParserService>();
builder.Services.AddSingleton<GeoLookupService>();

// Background workers
builder.Services.AddHostedService<EventConsumerWorker>();
builder.Services.AddHostedService<SessionBuilderWorker>();

// Auth
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DeepInsightDevKey_ChangeInProduction_32chars!!"))
        };

        // Support SignalR auth via query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDashboard", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    options.AddPolicy("AllowSDK", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .WithMethods("POST", "OPTIONS");
    });
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

// Response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// Initialize databases
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>() as ClickHouseEventStore;
    if (eventStore != null)
    {
        try { await eventStore.EnsureTablesAsync(); }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "ClickHouse initialization failed — will retry on first use");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseResponseCompression();

// Rate limiting for ingestion
app.UseMiddleware<RateLimitingMiddleware>();

// CORS — SDK endpoints use AllowSDK, dashboard uses AllowDashboard
app.UseCors("AllowDashboard");

app.UseAuthentication();
app.UseAuthorization();

// Map SDK ingestion with separate CORS
app.MapControllers();

app.MapHub<LiveSessionHub>("/hubs/live");

app.Run();
