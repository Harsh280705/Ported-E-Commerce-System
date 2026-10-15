using System.Net;
using System.Net.Http.Json;
using Ecommerce.Core.Dtos;
using Ecommerce.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Tests.Integration;

public class TestCatalogSeederTests(TestFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Seeder_InsertsDeterministicCatalog_And_IsIdempotent()
    {
        const int target = 100;
        int BeforeCount()
        {
            using var s = Factory.Services.CreateScope();
            return s.ServiceProvider.GetRequiredService<AppDbContext>()
                .Products.Count(p => p.Slug.StartsWith(TestCatalogSeeder.SlugPrefix));
        }

        var before = BeforeCount();
        var first = await TestCatalogSeeder.EnsureTestCatalogAsync(Factory.Services, targetCount: target);
        first.Inserted.Should().Be(Math.Max(0, target - before));

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var slugs = db.Products.Where(p => p.Slug.StartsWith(TestCatalogSeeder.SlugPrefix)).Select(p => p.Slug).ToList();
            slugs.Should().HaveCount(target);
            slugs.Distinct().Should().HaveCount(target);
            db.Products.Where(p => p.Slug.StartsWith(TestCatalogSeeder.SlugPrefix))
                .All(p => p.ImageUrl.StartsWith("/images/photos/") && p.Stock >= 0 && p.Price > 0)
                .Should().BeTrue();
        }

        var second = await TestCatalogSeeder.EnsureTestCatalogAsync(Factory.Services, targetCount: target);
        second.Inserted.Should().Be(0);
        second.TotalProducts.Should().Be(first.TotalProducts);
    }

    [Fact]
    public void DefaultTarget_Is2000() => TestCatalogSeeder.DefaultTarget.Should().Be(2000);

    [Fact]
    public async Task Admin_Products_Search_Filter_Sort()
    {
        await TestCatalogSeeder.EnsureTestCatalogAsync(Factory.Services, targetCount: 40);
        var admin = await AdminClientAsync();

        var search = await (await admin.GetAsync("/api/admin/products?search=headphones&pageSize=50"))
            .Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        search!.TotalCount.Should().BeGreaterThan(0);
        search.Items.Should().OnlyContain(p =>
            p.Name.Contains("headphones", StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains("headphones", StringComparison.OrdinalIgnoreCase));

        var cats = await (await admin.GetAsync("/api/admin/categories")).Content.ReadFromJsonAsync<List<CategoryDto>>();
        var cat = cats!.First();
        var filtered = await (await admin.GetAsync($"/api/admin/products?categoryId={cat.Id}&pageSize=50"))
            .Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        filtered!.Items.Should().OnlyContain(p => p.CategoryId == cat.Id);

        var sorted = await (await admin.GetAsync("/api/admin/products?sort=price_asc&pageSize=50"))
            .Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        sorted!.Items.Select(p => p.Price).Should().BeInAscendingOrder();

        // 2000+ products paginate in bounded pages
        var p1 = await (await admin.GetAsync("/api/admin/products?page=1&pageSize=20"))
            .Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        var p2 = await (await admin.GetAsync("/api/admin/products?page=2&pageSize=20"))
            .Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        p1!.Items.Select(p => p.Id).Should().NotIntersectWith(p2!.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Admin_Product_ImageUrl_Persisted_And_Validated()
    {
        var admin = await AdminClientAsync();
        var cats = await (await admin.GetAsync("/api/admin/categories")).Content.ReadFromJsonAsync<List<CategoryDto>>();
        var catId = cats!.First().Id;

        var created = await (await admin.PostAsJsonAsync("/api/admin/products",
            new AdminProductRequest("Image Widget", "image-widget-t1", "Has image", 999m, null, 10, "/images/products/image-widget-t1.svg", catId, true, false)))
            .Content.ReadFromJsonAsync<ProductDetailDto>();
        created!.ImageUrl.Should().Be("/images/products/image-widget-t1.svg");

        var bad = await admin.PostAsJsonAsync("/api/admin/products",
            new AdminProductRequest("Bad Image", "bad-image-t1", "Bad", 999m, null, 10, "not a url", catId, true, false));
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
