using System.Security.Claims;
using Ecommerce.Core;
using Ecommerce.Core.Dtos;
using FluentValidation;

namespace Ecommerce.Api.Endpoints;

public static class HttpHelpers
{
    public static int? CustomerId(this ClaimsPrincipal u)
    {
        var id = u.FindFirstValue(ClaimTypes.NameIdentifier) ?? u.FindFirstValue("sub");
        return int.TryParse(id, out var n) ? n : null;
    }
    public static bool IsAdmin(this ClaimsPrincipal u) =>
        u.IsInRole("Admin") || string.Equals(u.FindFirstValue("role"), "Admin", StringComparison.OrdinalIgnoreCase);

    public static string? AnonymousId(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue("X-Cart-Id", out var v) && !string.IsNullOrWhiteSpace(v)) return v.ToString().Trim()[..Math.Min(64, v.ToString().Trim().Length)];
        if (ctx.Request.Cookies.TryGetValue("cart_id", out var c) && !string.IsNullOrWhiteSpace(c)) return c.Trim();
        return null;
    }

    public static async Task<IResult> ValidateAsync<T>(IValidator<T> validator, T value)
    {
        var r = await validator.ValidateAsync(value);
        return r.IsValid ? null! : Results.ValidationProblem(r.ToDictionary());
    }
}

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");
        g.MapPost("/register", async (RegisterRequest req, IAuthService auth, IValidator<RegisterRequest> v, HttpContext ctx) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            try
            {
                var (a, refresh, exp) = await auth.RegisterAsync(req);
                SetRefreshCookie(ctx, refresh, exp);
                return Results.Created("/api/auth/me", a);
            }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        }).AllowAnonymous().RequireRateLimiting("auth");

        g.MapPost("/login", async (LoginRequest req, IAuthService auth, IValidator<LoginRequest> v, HttpContext ctx) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            try
            {
                var (a, refresh, exp) = await auth.LoginAsync(req);
                SetRefreshCookie(ctx, refresh, exp);
                return Results.Ok(a);
            }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        }).AllowAnonymous().RequireRateLimiting("auth");

        g.MapPost("/refresh", async (RefreshRequest? body, IAuthService auth, HttpContext ctx) =>
        {
            var token = body?.RefreshToken ?? (ctx.Request.Cookies.TryGetValue("refresh_token", out var c) ? c : null);
            try
            {
                var res = await auth.RefreshAsync(token);
                if (res == null) return Results.Problem("Invalid refresh token.", statusCode: 401);
                SetRefreshCookie(ctx, res.Value.RefreshToken, res.Value.RefreshExpires);
                return Results.Ok(res.Value.Auth);
            }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        }).AllowAnonymous();

        g.MapPost("/logout", async (RefreshRequest? body, IAuthService auth, HttpContext ctx) =>
        {
            var token = body?.RefreshToken ?? (ctx.Request.Cookies.TryGetValue("refresh_token", out var c) ? c : null);
            await auth.RevokeAsync(token);
            ctx.Response.Cookies.Delete("refresh_token");
            return Results.NoContent();
        }).RequireAuthorization();

        g.MapGet("/me", (ClaimsPrincipal u) =>
        {
            var id = u.CustomerId();
            if (id == null) return Results.Unauthorized();
            return Results.Ok(new UserDto(id.Value, u.FindFirstValue(ClaimTypes.Email) ?? "",
                u.FindFirstValue(ClaimTypes.GivenName) ?? "", u.FindFirstValue(ClaimTypes.Surname) ?? "",
                u.IsAdmin() ? "Admin" : "Customer"));
        }).RequireAuthorization();
    }

    private static void SetRefreshCookie(HttpContext ctx, string token, DateTime exp)
    {
        ctx.Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true, Secure = ctx.Request.IsHttps, SameSite = SameSiteMode.Lax, Expires = exp, Path = "/api/auth",
        });
    }
}

public static class CatalogEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api").WithTags("Catalog");
        g.MapGet("/categories", async (ICatalogService s) => Results.Ok(await s.GetCategoriesAsync())).AllowAnonymous();
        g.MapGet("/products", async (int? page, int? pageSize, int? categoryId, string? search, string? sort, ICatalogService s) =>
            Results.Ok(await s.SearchProductsAsync(page ?? 1, pageSize ?? 12, categoryId, search, sort))).AllowAnonymous();
        g.MapGet("/products/{id:int}", async (int id, ICatalogService s) =>
            await s.GetProductAsync(id) is { } p ? Results.Ok(p) : Results.Problem("Product not found.", statusCode: 404)).AllowAnonymous();
    }
}

public static class CartEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/cart").WithTags("Cart");
        g.MapGet("/", async (HttpContext ctx, ICartService s) =>
            Results.Ok(await s.GetCartAsync(ctx.User.CustomerId(), HttpHelpers.AnonymousId(ctx)))).AllowAnonymous();
        g.MapPost("/items", async (AddCartItemRequest req, HttpContext ctx, ICartService s, IValidator<AddCartItemRequest> v) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            try { return Results.Ok(await s.AddItemAsync(ctx.User.CustomerId(), EnsureAnon(ctx), req.ProductId, req.Quantity)); }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        }).AllowAnonymous();
        g.MapPut("/items/{productId:int}", async (int productId, UpdateCartItemRequest req, HttpContext ctx, ICartService s, IValidator<UpdateCartItemRequest> v) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            try { return Results.Ok(await s.UpdateItemAsync(ctx.User.CustomerId(), EnsureAnon(ctx), productId, req.Quantity)); }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        }).AllowAnonymous();
        g.MapDelete("/items/{productId:int}", async (int productId, HttpContext ctx, ICartService s) =>
        {
            try { return Results.Ok(await s.RemoveItemAsync(ctx.User.CustomerId(), EnsureAnon(ctx), productId)); }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        }).AllowAnonymous();
        g.MapDelete("/", async (HttpContext ctx, ICartService s) =>
        {
            await s.ClearCartAsync(ctx.User.CustomerId(), HttpHelpers.AnonymousId(ctx));
            return Results.NoContent();
        }).AllowAnonymous();
        g.MapPost("/merge", async (HttpContext ctx, ICartService s) =>
        {
            var id = ctx.User.CustomerId();
            if (id == null) return Results.Unauthorized();
            return Results.Ok(await s.MergeAsync(id.Value, HttpHelpers.AnonymousId(ctx)));
        }).RequireAuthorization();
    }

    private static string? EnsureAnon(HttpContext ctx)
    {
        var existing = HttpHelpers.AnonymousId(ctx);
        if (existing != null) return existing;
        var fresh = Guid.NewGuid().ToString("N");
        ctx.Response.Cookies.Append("cart_id", fresh, new CookieOptions { SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30), Path = "/" });
        ctx.Response.Headers["X-Cart-Id"] = fresh;
        return fresh;
    }
}

public static class OrderEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();
        g.MapPost("/", async (CheckoutRequest req, HttpContext ctx, IOrderService s, IValidator<CheckoutRequest> v) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            var id = ctx.User.CustomerId();
            if (id == null) return Results.Unauthorized();
            try
            {
                var o = await s.PlaceOrderAsync(id.Value, req);
                return Results.Created($"/api/orders/{o.Id}", o);
            }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        });
        g.MapGet("/", async (HttpContext ctx, IOrderService s) =>
        {
            var id = ctx.User.CustomerId();
            if (id == null) return Results.Unauthorized();
            return Results.Ok(await s.GetHistoryAsync(id.Value, ctx.User.IsAdmin()));
        });
        g.MapGet("/{id:int}", async (int id, HttpContext ctx, IOrderService s) =>
        {
            var cid = ctx.User.CustomerId();
            if (cid == null) return Results.Unauthorized();
            try
            {
                var o = await s.GetDetailAsync(cid.Value, id, ctx.User.IsAdmin());
                return o == null ? Results.Problem("Order not found.", statusCode: 404) : Results.Ok(o);
            }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        });
    }
}

public static class AccountEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/account").WithTags("Account").RequireAuthorization();
        g.MapGet("/profile", async (HttpContext ctx, IAccountService s) =>
        {
            var id = ctx.User.CustomerId(); if (id == null) return Results.Unauthorized();
            var p = await s.GetProfileAsync(id.Value);
            return p == null ? Results.Problem("Not found.", statusCode: 404) : Results.Ok(p);
        });
        g.MapPut("/profile", async (UpdateProfileRequest req, HttpContext ctx, IAccountService s, IValidator<UpdateProfileRequest> v) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            var id = ctx.User.CustomerId(); if (id == null) return Results.Unauthorized();
            var p = await s.UpdateProfileAsync(id.Value, req);
            return p == null ? Results.Problem("Not found.", statusCode: 404) : Results.Ok(p);
        });
        g.MapGet("/addresses", async (HttpContext ctx, IAccountService s) =>
        {
            var id = ctx.User.CustomerId(); if (id == null) return Results.Unauthorized();
            return Results.Ok(await s.GetAddressesAsync(id.Value));
        });
        g.MapPost("/addresses", async (CreateAddressRequest req, HttpContext ctx, IAccountService s, IValidator<CreateAddressRequest> v) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            var id = ctx.User.CustomerId(); if (id == null) return Results.Unauthorized();
            var a = await s.CreateAddressAsync(id.Value, req);
            return Results.Created($"/api/account/addresses/{a.Id}", a);
        });
        g.MapPut("/addresses/{id:int}", async (int id, UpdateAddressRequest req, HttpContext ctx, IAccountService s) =>
        {
            var cid = ctx.User.CustomerId(); if (cid == null) return Results.Unauthorized();
            var a = await s.UpdateAddressAsync(cid.Value, id, req);
            return a == null ? Results.Problem("Address not found.", statusCode: 404) : Results.Ok(a);
        });
        g.MapDelete("/addresses/{id:int}", async (int id, HttpContext ctx, IAccountService s) =>
        {
            var cid = ctx.User.CustomerId(); if (cid == null) return Results.Unauthorized();
            return await s.DeleteAddressAsync(cid.Value, id) ? Results.NoContent() : Results.Problem("Address not found.", statusCode: 404);
        });
    }
}

public static class AdminEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization("Admin");
        g.MapGet("/dashboard", async (IAdminService s) => Results.Ok(await s.GetDashboardAsync()));
        g.MapGet("/products", async (int? page, int? pageSize, int? categoryId, string? search, string? sort, IAdminService s) =>
            Results.Ok(await s.GetProductsAsync(page ?? 1, pageSize ?? 20, categoryId, search, sort)));
        g.MapPost("/products", async (AdminProductRequest req, IAdminService s, IValidator<AdminProductRequest> v) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            try { var p = await s.CreateProductAsync(req); return Results.Created($"/api/products/{p!.Id}", p); }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        });
        g.MapPut("/products/{id:int}", async (int id, AdminProductRequest req, IAdminService s, IValidator<AdminProductRequest> v) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            try
            {
                var p = await s.UpdateProductAsync(id, req);
                return p == null ? Results.Problem("Product not found.", statusCode: 404) : Results.Ok(p);
            }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        });
        g.MapDelete("/products/{id:int}", async (int id, IAdminService s) =>
        {
            try { return await s.DeleteProductAsync(id) ? Results.NoContent() : Results.Problem("Product not found.", statusCode: 404); }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        });
        g.MapGet("/categories", async (IAdminService s) => Results.Ok(await s.GetCategoriesAsync()));
        g.MapPost("/categories", async (AdminCategoryRequest req, IAdminService s, IValidator<AdminCategoryRequest> v) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            try { var c = await s.CreateCategoryAsync(req); return Results.Created($"/api/categories", c); }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        });
        g.MapPut("/categories/{id:int}", async (int id, AdminCategoryRequest req, IAdminService s, IValidator<AdminCategoryRequest> v) =>
        {
            var err = await HttpHelpers.ValidateAsync(v, req);
            if (err != null) return err;
            try
            {
                var c = await s.UpdateCategoryAsync(id, req);
                return c == null ? Results.Problem("Category not found.", statusCode: 404) : Results.Ok(c);
            }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        });
        g.MapDelete("/categories/{id:int}", async (int id, IAdminService s) =>
        {
            try { return await s.DeleteCategoryAsync(id) ? Results.NoContent() : Results.Problem("Category not found.", statusCode: 404); }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        });
        g.MapGet("/orders", async (IAdminService s) => Results.Ok(await s.GetOrdersAsync()));
        g.MapPut("/orders/{id:int}/status", async (int id, UpdateOrderStatusRequest req, IAdminService s) =>
        {
            try
            {
                var o = await s.UpdateOrderStatusAsync(id, req.Status);
                return o == null ? Results.Problem("Order not found.", statusCode: 404) : Results.Ok(o);
            }
            catch (DomainException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        });
    }
}
