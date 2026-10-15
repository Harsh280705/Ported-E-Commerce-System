using System.Text;
using System.Threading.RateLimiting;
using Ecommerce.Api.Endpoints;
using Ecommerce.Api.Services;
using Ecommerce.Api.Validation;
using Ecommerce.Core;
using Ecommerce.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

var conn = builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
    ?? "Server=localhost,1433;Database=EcommerceDb;User Id=sa;Password=ChangeMe!234567890abc;TrustServerCertificate=True";
var testSqlite = builder.Configuration["TestSqlite"];
if (testSqlite != null)
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(testSqlite));
else
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(conn));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

static string CfgOrEnv(IConfiguration cfg, string key, string env, string fallback)
{
    var v = cfg[key];
    // appsettings.json keeps ${PLACEHOLDER} tokens; treat unexpanded tokens as missing.
    if (string.IsNullOrWhiteSpace(v) || v.StartsWith("${"))
        v = Environment.GetEnvironmentVariable(env);
    if (string.IsNullOrWhiteSpace(v) || v.StartsWith("${"))
        v = fallback;
    return v;
}

const string DevKey = "dev-only-signing-key-min-32-chars-0123456789abcdef";
var jwtKey = CfgOrEnv(builder.Configuration, "Jwt:Key", "JWT_KEY", DevKey);
var issuer = CfgOrEnv(builder.Configuration, "Jwt:Issuer", "JWT_ISSUER", "ecommerce-api");
var audience = CfgOrEnv(builder.Configuration, "Jwt:Audience", "JWT_AUDIENCE", "ecommerce-spa");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = audience,
        ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
    };
});
builder.Services.AddAuthorization(opts => opts.AddPolicy("Admin", p => p.RequireRole("Admin")));

var authPermit = builder.Environment.IsEnvironment("Testing") ? 1000 : 20;
builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermit, Window = TimeSpan.FromMinutes(1) }));
    o.RejectionStatusCode = 429;
});
builder.Services.AddCors(o => o.AddPolicy("spa", p => p
    .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173", "http://localhost:8080"])
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("db");
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsEnvironment("Testing"))
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler();
app.UseRateLimiter();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow })).AllowAnonymous();
app.MapHealthChecks("/health/db");
app.MapOpenApi("/openapi/v1.json");
if (app.Environment.IsDevelopment())
{
    // Minimal Swagger UI (no Swashbuckle dependency): serves CDN-backed UI reading the built-in OpenAPI document.
    app.MapGet("/swagger", () => Results.Content("""
        <!doctype html><html><head><meta charset="utf-8"><title>Ecommerce API — Swagger UI</title>
        <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"></head>
        <body><div id="ui"></div>
        <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
        <script>SwaggerUIBundle({ url: '/openapi/v1.json', dom_id: '#ui' });</script>
        </body></html>
        """, "text/html")).AllowAnonymous();
}

AuthEndpoints.Map(app);
CatalogEndpoints.Map(app);
CartEndpoints.Map(app);
OrderEndpoints.Map(app);
AccountEndpoints.Map(app);
AdminEndpoints.Map(app);

// Apply migrations + seed safely (single-instance; skips when DB unreachable in test envs)
try { await SeedData.EnsureSeededAsync(app.Services); }
catch (Exception ex) { Log.Warning(ex, "Startup seeding skipped: {Msg}", ex.Message); }

// Explicit development/test-data generation only: never automatic in production.
// Enable with configuration SeedTestCatalog=true (env SEEDTESTCATALOG is NOT used;
// set SeedTestCatalog via environment variable "SeedTestCatalog").
if (string.Equals(app.Configuration["SeedTestCatalog"], "true", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        var r = await TestCatalogSeeder.EnsureTestCatalogAsync(app.Services);
        Log.Information("Test catalog seeding: inserted {Inserted}, total {Total}, categories {Cats}, images normalized {Norm}",
            r.Inserted, r.TotalProducts, r.Categories, r.ImagesNormalized);
    }
    catch (Exception ex) { Log.Warning(ex, "Test catalog seeding failed: {Msg}", ex.Message); }
}

app.Run();

public partial class Program { }
