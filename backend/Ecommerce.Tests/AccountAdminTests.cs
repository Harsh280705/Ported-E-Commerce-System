using System.Net;
using System.Net.Http.Json;
using Ecommerce.Core.Dtos;
using FluentAssertions;

namespace Ecommerce.Tests.Integration;

public class AccountAdminTests(TestFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Profile_Get_Update()
    {
        var authed = await RegisterLoginAsync();
        (await authed.GetAsync("/api/account/profile")).StatusCode.Should().Be(HttpStatusCode.OK);
        var upd = await authed.PutAsJsonAsync("/api/account/profile", new UpdateProfileRequest("New", "Name"));
        upd.StatusCode.Should().Be(HttpStatusCode.OK);
        (await upd.Content.ReadFromJsonAsync<ProfileDto>())!.FirstName.Should().Be("New");
    }

    [Fact]
    public async Task Addresses_Crud_DefaultHandling()
    {
        var authed = await RegisterLoginAsync();
        var created = await (await authed.PostAsJsonAsync("/api/account/addresses",
            new CreateAddressRequest("Test User", "1 Main St", null, "Mumbai", "Maharashtra", "400001", "India", null, true)))
            .Content.ReadFromJsonAsync<AddressDto>();
        created!.IsDefault.Should().BeTrue();

        var second = await (await authed.PostAsJsonAsync("/api/account/addresses",
            new CreateAddressRequest("Test User", "2 Main St", null, "Mumbai", "Maharashtra", "400002", "India", null, true)))
            .Content.ReadFromJsonAsync<AddressDto>();
        second!.IsDefault.Should().BeTrue();

        var list = await (await authed.GetAsync("/api/account/addresses")).Content.ReadFromJsonAsync<List<AddressDto>>();
        list!.Count(a => a.IsDefault).Should().Be(1);

        (await authed.DeleteAsync($"/api/account/addresses/{second.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await (await authed.GetAsync("/api/account/addresses")).Content.ReadFromJsonAsync<List<AddressDto>>();
        after!.Count(a => a.IsDefault).Should().Be(1);
    }

    [Fact]
    public async Task Addresses_Isolated_PerCustomer()
    {
        var a = await RegisterLoginAsync();
        var created = await (await a.PostAsJsonAsync("/api/account/addresses",
            new CreateAddressRequest("A", "1 St", null, "C", "S", "111111", "India", null, true))).Content.ReadFromJsonAsync<AddressDto>();
        var b = await RegisterLoginAsync();
        (await b.DeleteAsync($"/api/account/addresses/{created!.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_Dashboard_RequiresAdmin()
    {
        (await Client.GetAsync("/api/admin/dashboard")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var user = await RegisterLoginAsync();
        (await user.GetAsync("/api/admin/dashboard")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var admin = await AdminClientAsync();
        var dash = await admin.GetAsync("/api/admin/dashboard");
        dash.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await dash.Content.ReadFromJsonAsync<DashboardDto>();
        body!.TotalProducts.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Admin_Product_Crud()
    {
        var admin = await AdminClientAsync();
        var cats = await (await admin.GetAsync("/api/admin/categories")).Content.ReadFromJsonAsync<List<CategoryDto>>();
        var catId = cats!.First().Id;

        var created = await (await admin.PostAsJsonAsync("/api/admin/products",
            new AdminProductRequest("Admin Widget", "", "Created by test", 1234m, null, 25, "/images/placeholder.png", catId, true, false)))
            .Content.ReadFromJsonAsync<ProductDetailDto>();
        created!.Name.Should().Be("Admin Widget");

        var updated = await (await admin.PutAsJsonAsync($"/api/admin/products/{created.Id}",
            new AdminProductRequest("Admin Widget v2", created.Slug, "Updated", 1500m, 1800m, 20, "/images/placeholder.png", catId, true, true)))
            .Content.ReadFromJsonAsync<ProductDetailDto>();
        updated!.Price.Should().Be(1500m);

        (await admin.DeleteAsync($"/api/admin/products/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/products/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_Product_Delete_Blocked_WithHistory()
    {
        var admin = await AdminClientAsync();
        var authed = await RegisterLoginAsync();
        var list = await (await Client.GetAsync("/api/products?pageSize=5")).Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        var pid = list!.Items.First(p => p.InStock).Id;
        UseCart(authed, NewCartId());
        var addr = new AddressInput("T", "L1", null, "C", "S", "111111", "India", null);
        await authed.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(pid, 1));
        await authed.PostAsJsonAsync("/api/orders", new CheckoutRequest(addr, addr, null, null, "standard", "cod", Guid.NewGuid().ToString("N")));
        (await admin.DeleteAsync($"/api/admin/products/{pid}")).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Admin_Category_Delete_Blocked_WhenProductsExist()
    {
        var admin = await AdminClientAsync();
        var cats = await (await admin.GetAsync("/api/admin/categories")).Content.ReadFromJsonAsync<List<CategoryDto>>();
        (await admin.DeleteAsync($"/api/admin/categories/{cats!.First(c => c.ProductCount > 0).Id}")).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Admin_Order_StatusFlow()
    {
        var admin = await AdminClientAsync();
        var authed = await RegisterLoginAsync();
        var list = await (await Client.GetAsync("/api/products?pageSize=5")).Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        var pid = list!.Items.First(p => p.InStock).Id;
        UseCart(authed, NewCartId());
        var addr = new AddressInput("T", "L1", null, "C", "S", "111111", "India", null);
        await authed.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(pid, 1));
        var order = await (await authed.PostAsJsonAsync("/api/orders",
            new CheckoutRequest(addr, addr, null, null, "standard", "cod", Guid.NewGuid().ToString("N")))).Content.ReadFromJsonAsync<OrderDto>();

        var bad = await admin.PutAsJsonAsync($"/api/admin/orders/{order!.Id}/status", new UpdateOrderStatusRequest("Delivered"));
        bad.StatusCode.Should().Be(HttpStatusCode.OK); // Placed -> Delivered is forward jump, allowed

        var invalid = await admin.PutAsJsonAsync($"/api/admin/orders/{order.Id}/status", new UpdateOrderStatusRequest("Placed"));
        invalid.StatusCode.Should().Be(HttpStatusCode.Conflict); // backward transition rejected
    }
}
