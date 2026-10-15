using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Core.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ecommerce.Tests.Integration;

public class CartOrderTests(TestFactory factory) : ApiTestBase(factory)
{
    private static AddressInput Addr() => new("Test User", "42 MG Road", null, "Bengaluru", "Karnataka", "560001", "India", "9876543210");

    private async Task<int> FirstInStockProductIdAsync()
    {
        var page = await (await Client.GetAsync("/api/products?pageSize=20")).Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        return page!.Items.First(p => p.InStock).Id;
    }

    [Fact]
    public async Task GuestCart_Add_Update_Remove()
    {
        var cart = NewCartClient();
        var pid = await FirstInStockProductIdAsync();

        var add = await cart.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(pid, 2));
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await add.Content.ReadFromJsonAsync<CartDto>();
        dto!.ItemCount.Should().Be(2);
        dto.SubTotal.Should().BeGreaterThan(0);

        var upd = await cart.PutAsJsonAsync($"/api/cart/items/{pid}", new UpdateCartItemRequest(3));
        (await upd.Content.ReadFromJsonAsync<CartDto>())!.ItemCount.Should().Be(3);

        var del = await cart.DeleteAsync($"/api/cart/items/{pid}");
        (await del.Content.ReadFromJsonAsync<CartDto>())!.ItemCount.Should().Be(0);
    }

    [Fact]
    public async Task GuestCart_InvalidQuantity_400()
    {
        var cart = NewCartClient();
        var pid = await FirstInStockProductIdAsync();
        (await cart.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(pid, 100))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GuestCart_OutOfStock_409()
    {
        var page = await (await Client.GetAsync("/api/products?pageSize=20")).Content.ReadFromJsonAsync<PagedResultDto<ProductCardDto>>();
        var oos = page!.Items.FirstOrDefault(p => !p.InStock);
        oos.Should().NotBeNull();
        var cart = NewCartClient();
        (await cart.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(oos!.Id, 1))).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GuestCart_Merges_OnLogin()
    {
        var pid = await FirstInStockProductIdAsync();
        var guestId = NewCartId();
        var guest = NewCartClient(guestId);
        (await guest.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(pid, 2))).StatusCode.Should().Be(HttpStatusCode.OK);

        var authed = await RegisterLoginAsync();
        UseCart(authed, guestId);
        var merge = await authed.PostAsync("/api/cart/merge", null);
        merge.StatusCode.Should().Be(HttpStatusCode.OK);
        (await merge.Content.ReadFromJsonAsync<CartDto>())!.ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task Checkout_CreatesOrder_DecrementsStock_ClearsCart()
    {
        var authed = await RegisterLoginAsync();
        var pid = await FirstInStockProductIdAsync();
        var before = await (await Client.GetAsync($"/api/products/{pid}")).Content.ReadFromJsonAsync<ProductDetailDto>();
        UseCart(authed, NewCartId());
        (await authed.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(pid, 1))).StatusCode.Should().Be(HttpStatusCode.OK);

        var key = Guid.NewGuid().ToString("N");
        var checkout = new CheckoutRequest(Addr(), Addr(), null, null, "standard", "cod", key);
        var placed = await authed.PostAsJsonAsync("/api/orders", checkout);
        placed.StatusCode.Should().Be(HttpStatusCode.Created);
        placed.Headers.Location.Should().NotBeNull();
        var order = await placed.Content.ReadFromJsonAsync<OrderDto>();
        order!.Total.Should().Be(order.SubTotal + order.ShippingCost + order.TaxAmount);
        order.Items.Should().ContainSingle(i => i.ProductId == pid);

        // Idempotent retry returns same order
        var retry = await authed.PostAsJsonAsync("/api/orders", checkout);
        retry.StatusCode.Should().Be(HttpStatusCode.Created);
        (await retry.Content.ReadFromJsonAsync<OrderDto>())!.Id.Should().Be(order.Id);

        // Stock decremented
        var after = await (await Client.GetAsync($"/api/products/{pid}")).Content.ReadFromJsonAsync<ProductDetailDto>();
        after!.Stock.Should().Be(before!.Stock - 1);

        // Cart cleared
        (await authed.GetAsync("/api/cart")).Content.ReadFromJsonAsync<CartDto>().Result!.ItemCount.Should().Be(0);

        // History + detail
        var hist = await (await authed.GetAsync("/api/orders")).Content.ReadFromJsonAsync<List<OrderHistoryDto>>();
        hist.Should().Contain(h => h.Id == order.Id);
        (await authed.GetAsync($"/api/orders/{order.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Checkout_EmptyCart_400()
    {
        var authed = await RegisterLoginAsync();
        UseCart(authed, NewCartId());
        var r = await authed.PostAsJsonAsync("/api/orders", new CheckoutRequest(Addr(), Addr(), null, null, "standard", "cod", Guid.NewGuid().ToString("N")));
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_InvalidShipping_400()
    {
        var authed = await RegisterLoginAsync();
        var pid = await FirstInStockProductIdAsync();
        UseCart(authed, NewCartId());
        await authed.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(pid, 1));
        var r = await authed.PostAsJsonAsync("/api/orders", new CheckoutRequest(Addr(), Addr(), null, null, "teleport", "cod", null));
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Orders_Unauthorized_401() =>
        (await Client.GetAsync("/api/orders")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Order_Ownership_Enforced()
    {
        var a = await RegisterLoginAsync();
        var pid = await FirstInStockProductIdAsync();
        UseCart(a, NewCartId());
        await a.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(pid, 1));
        var order = await (await a.PostAsJsonAsync("/api/orders",
            new CheckoutRequest(Addr(), Addr(), null, null, "standard", "cod", Guid.NewGuid().ToString("N")))).Content.ReadFromJsonAsync<OrderDto>();

        var b = await RegisterLoginAsync();
        (await b.GetAsync($"/api/orders/{order!.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient NewCartClient(string? cartId = null)
    {
        var c = Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        UseCart(c, cartId ?? NewCartId());
        return c;
    }
}
