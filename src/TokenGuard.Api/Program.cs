using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TokenGuard.Api.Configuration;
using TokenGuard.Api.Middleware;
using TokenGuard.Core.Interfaces;
using TokenGuard.Infrastructure.Data;
using TokenGuard.Infrastructure.Repositories;
using TokenGuard.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<ProvidersOptions>(
    builder.Configuration.GetSection(ProvidersOptions.Section));

// Database
var dbPath = builder.Configuration.GetConnectionString("Sqlite") ?? "tokenguard.db";
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite($"Data Source={dbPath}"));

// Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? builder.Configuration["Redis:ConnectionString"]
    ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));

// Core services
builder.Services.AddScoped<IUsageRepository, UsageRepository>();
builder.Services.AddScoped<IBudgetService, RedisBudgetService>();
builder.Services.AddSingleton<ITokenCountingService, SimpleTokenCountingService>();

// HTTP client for upstream proxying
builder.Services.AddHttpClient("upstream", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// JWT Auth
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "CHANGE_ME_32_CHARS_MIN_SECRET_KEY_1";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TokenGuard";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// CORS for dashboard
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Apply migrations and create DB on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Proxy middleware for /v1/proxy/...
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/v1/proxy"),
    appBranch => appBranch.UseMiddleware<ProxyMiddleware>());

app.MapControllers();

// Auth token generation endpoint (dev convenience)
app.MapPost("/api/auth/token", (TokenRequest req, IConfiguration config) =>
{
    var secret = config["Jwt:Secret"] ?? "CHANGE_ME_32_CHARS_MIN_SECRET_KEY_1";
    var issuer = config["Jwt:Issuer"] ?? "TokenGuard";

    if (req.AdminSecret != secret)
        return Results.Unauthorized();

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: issuer,
        claims: [new System.Security.Claims.Claim("role", "admin")],
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: creds);

    var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = tokenString });
});

app.Run();

record TokenRequest(string AdminSecret);
