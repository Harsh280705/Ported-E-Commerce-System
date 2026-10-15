using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Core.Dtos;
using Ecommerce.Core.Entities;
using Ecommerce.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Tests.Integration;

public class TestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestMemDb_" + Guid.NewGuid().ToString("N");
    private SqliteConnection? _conn;
    private int _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Single provider registration: Program.cs picks SQLite when TestSqlite is set,
        // avoiding the dual SqlServer+Sqlite provider conflict.
        // Unique DB per factory instance: xUnit runs test classes in parallel.
        builder.UseSetting("TestSqlite", $"DataSource={_dbName};Mode=Memory;Cache=Shared");
        builder.ConfigureServices(services =>
        {
            _conn = new SqliteConnection($"DataSource={_dbName};Mode=Memory;Cache=Shared");
            _conn.Open();
        });
    }

    /// <summary>Idempotent: creates schema (startup SQL-Server migration is skipped on SQLite) and seeds minimal data.</summary>
    public async Task EnsureReadyAsync()
    {
        if (Interlocked.Exchange(ref _seeded, 1) == 1)
        {
            using var s = Services.CreateScope();
            await s.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
            return;
        }
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "Test Fashion", Slug = "test-fashion", Description = "Test cat", DisplayOrder = 1 },
                new Category { Name = "Test Electronics", Slug = "test-electronics", Description = "Test cat", DisplayOrder = 2 });
            await db.SaveChangesAsync();
        }
        if (!await db.Products.AnyAsync())
        {
            var c1 = await db.Categories.FirstAsync(c => c.Slug == "test-fashion");
            var c2 = await db.Categories.FirstAsync(c => c.Slug == "test-electronics");
            db.Products.AddRange(
                new Product { Sku = "T-00001", Name = "Blue Test Shirt", Slug = "blue-test-shirt", Description = "A blue shirt for testing", Price = 500m, Stock = 50, ImageUrl = "/images/placeholder.png", CategoryId = c1.Id },
                new Product { Sku = "T-00002", Name = "Red Test Phone", Slug = "red-test-phone", Description = "A red phone for testing", Price = 9999m, CompareAtPrice = 12999m, Stock = 10, ImageUrl = "/images/placeholder.png", CategoryId = c2.Id, IsFeatured = true },
                new Product { Sku = "T-00003", Name = "Green Test Lamp", Slug = "green-test-lamp", Description = "Out of stock lamp", Price = 899m, Stock = 0, ImageUrl = "/images/placeholder.png", CategoryId = c1.Id });
            await db.SaveChangesAsync();
        }
        var hasher = new PasswordHasher<Customer>();
        if (!await db.Customers.AnyAsync(c => c.Email == "testadmin@example.com"))
        {
            var admin = new Customer { Email = "testadmin@example.com", FirstName = "Test", LastName = "Admin", Role = "Admin" };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin!123");
            db.Customers.Add(admin);
            await db.SaveChangesAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _conn?.Dispose();
    }
}

public abstract class ApiTestBase : IClassFixture<TestFactory>
{
    protected static readonly WebApplicationFactoryClientOptions NoCookies = new() { HandleCookies = false };
    protected readonly HttpClient Client;
    protected readonly TestFactory Factory;
    protected ApiTestBase(TestFactory factory)
    {
        Factory = factory;
        // No automatic cookie handling: refresh/cart cookies are asserted and replayed manually.
        Client = factory.CreateClient(NoCookies);
        factory.EnsureReadyAsync().GetAwaiter().GetResult();
    }

    protected static string NewEmail(string prefix = "user") => $"{prefix}_{Guid.NewGuid():N}@example.com";
    protected static string NewCartId() => Guid.NewGuid().ToString("N");

    protected async Task<HttpClient> RegisterLoginAsync(string? email = null, string password = "Password!123")
    {
        email ??= NewEmail();
        var reg = await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password, "Test", "User"));
        reg.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        var authed = Factory.CreateClient(NoCookies);
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return authed;
    }

    protected async Task<HttpClient> AdminClientAsync()
    {
        var login = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("testadmin@example.com", "Admin!123"));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();
        var c = Factory.CreateClient(NoCookies);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return c;
    }

    protected static void UseCart(HttpClient c, string cartId)
    {
        c.DefaultRequestHeaders.Remove("X-Cart-Id");
        c.DefaultRequestHeaders.Add("X-Cart-Id", cartId);
    }
}
