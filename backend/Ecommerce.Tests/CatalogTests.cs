using System.Net;
using System.Net.Http.Json;
using Ecommerce.Core.Dtos;
using FluentAssertions;

namespace Ecommerce.Tests.Integration;

public class CatalogTests(TestFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Categories_Public()
    {
        var r = await Client.GetAsync("/api/categories");
        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var cats = await r.Content.ReadFromJsonAsync<List<CategoryDto>>();
        cats.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Products_Pagination_Metadata()
    {
        var r = await Client.GetAsync("/api/products?page=1&pageSize=2");
        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await r.Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(2);
        page.TotalCount.Should().BeGreaterThan(0);
        page.TotalPages.Should().BeGreaterThan(0);
        page.Items.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task Products_Search_Filters_ByName()
    {
        var r = await Client.GetAsync("/api/products?search=phone");
        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await r.Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        page!.Items.Should().OnlyContain(p =>
            p.Name.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains("phone", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Products_CategoryFilter()
    {
        var cats = await (await Client.GetAsync("/api/categories")).Content.ReadFromJsonAsync<List<CategoryDto>>();
        var cat = cats!.First();
        var r = await Client.GetAsync($"/api/products?categoryId={cat.Id}");
        var page = await r.Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        page!.Items.Should().OnlyContain(p => p.CategoryId == cat.Id);
    }

    [Fact]
    public async Task Products_Sort_PriceAsc_IsOrdered()
    {
        var r = await Client.GetAsync("/api/products?sort=price_asc&pageSize=10");
        var page = await r.Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        page!.Items.Select(p => p.Price).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Product_Detail_And_NotFound()
    {
        var list = await (await Client.GetAsync("/api/products?pageSize=1")).Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        var id = list!.Items.First().Id;
        var ok = await Client.GetAsync($"/api/products/{id}");
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.GetAsync("/api/products/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Health_Endpoint_Ok()
    {
        (await Client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApi_Json_Served()
    {
        (await Client.GetAsync("/openapi/v1.json")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
