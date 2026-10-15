using Ecommerce.Core.Dtos;
using Ecommerce.Core.Entities;

namespace Ecommerce.Core;

// Centralised pricing preserved from legacy (Mapping.PricingRules).
public static class PricingRules
{
    public static readonly string[] ShippingMethods = ["standard", "express", "priority"];
    public static readonly string[] PaymentMethods = ["cod", "card_demo", "upi_demo", "netbanking_demo"];

    public static decimal ShippingFor(decimal subtotal, string? method)
    {
        if (subtotal <= 0) return 0;
        var m = (method ?? "standard").ToLowerInvariant();
        if (m == "express") return subtotal >= 150 ? 0 : 12.99m;
        if (m == "priority") return subtotal >= 200 ? 0 : 24.99m;
        return subtotal >= 75 ? 0 : 5.99m;
    }

    public static decimal TaxFor(decimal subtotal) => Math.Round(subtotal * 0.08m, 2);

    public static int? DiscountPct(decimal price, decimal? compareAt)
    {
        if (compareAt.HasValue && compareAt.Value > price && compareAt.Value > 0)
            return (int)Math.Round((double)((compareAt.Value - price) / compareAt.Value * 100));
        return null;
    }

    public static readonly string[] ValidSorts = ["recommended", "price_asc", "price_desc", "name_asc", "name_desc", "newest"];
    public static readonly string[] ValidStatuses = ["Placed", "Processing", "Shipped", "Delivered", "Completed", "Cancelled", "Refunded"];

    // Forward-only transitions (Cancelled allowed from Placed/Processing).
    public static bool IsValidTransition(string from, string to)
    {
        if (from == to) return true;
        var order = new[] { "Placed", "Processing", "Shipped", "Delivered", "Completed" };
        if (to == "Cancelled") return from == "Placed" || from == "Processing";
        if (to == "Refunded") return from == "Delivered" || from == "Completed" || from == "Shipped";
        var fi = Array.IndexOf(order, from); var ti = Array.IndexOf(order, to);
        return fi >= 0 && ti > fi;
    }
}

public interface ITokenService
{
    string CreateAccessToken(Customer customer);
    string GenerateRefreshToken();
    string HashToken(string token);
}

public interface IAuthService
{
    Task<(AuthResponse Auth, string RefreshToken, DateTime RefreshExpires)> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
    Task<(AuthResponse Auth, string RefreshToken, DateTime RefreshExpires)> LoginAsync(LoginRequest req, CancellationToken ct = default);
    Task<(AuthResponse Auth, string RefreshToken, DateTime RefreshExpires)?> RefreshAsync(string? refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string? refreshToken, CancellationToken ct = default);
    Task RevokeAllAsync(int customerId, CancellationToken ct = default);
}

public interface ICatalogService
{
    Task<IList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<PagedResultDto<ProductCardDto>> SearchProductsAsync(int page, int pageSize, int? categoryId, string? search, string? sort, CancellationToken ct = default);
    Task<ProductDetailDto?> GetProductAsync(int id, CancellationToken ct = default);
}

public interface ICartService
{
    Task<CartDto> GetCartAsync(int? customerId, string? anonymousId, CancellationToken ct = default);
    Task<CartDto> AddItemAsync(int? customerId, string? anonymousId, int productId, int quantity, CancellationToken ct = default);
    Task<CartDto> UpdateItemAsync(int? customerId, string? anonymousId, int productId, int quantity, CancellationToken ct = default);
    Task<CartDto> RemoveItemAsync(int? customerId, string? anonymousId, int productId, CancellationToken ct = default);
    Task ClearCartAsync(int? customerId, string? anonymousId, CancellationToken ct = default);
    Task<CartDto> MergeAsync(int customerId, string? anonymousId, CancellationToken ct = default);
}

public interface IOrderService
{
    Task<OrderDto> PlaceOrderAsync(int customerId, CheckoutRequest req, CancellationToken ct = default);
    Task<IList<OrderHistoryDto>> GetHistoryAsync(int customerId, bool isAdmin, CancellationToken ct = default);
    Task<OrderDto?> GetDetailAsync(int customerId, int id, bool isAdmin, CancellationToken ct = default);
}

public interface IAccountService
{
    Task<ProfileDto?> GetProfileAsync(int customerId, CancellationToken ct = default);
    Task<ProfileDto?> UpdateProfileAsync(int customerId, UpdateProfileRequest req, CancellationToken ct = default);
    Task<IList<AddressDto>> GetAddressesAsync(int customerId, CancellationToken ct = default);
    Task<AddressDto> CreateAddressAsync(int customerId, CreateAddressRequest req, CancellationToken ct = default);
    Task<AddressDto?> UpdateAddressAsync(int customerId, int id, UpdateAddressRequest req, CancellationToken ct = default);
    Task<bool> DeleteAddressAsync(int customerId, int id, CancellationToken ct = default);
}

public interface IAdminService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<PagedResultDto<ProductCardDto>> GetProductsAsync(int page, int pageSize, int? categoryId, string? search, string? sort, CancellationToken ct = default);
    Task<ProductDetailDto?> CreateProductAsync(AdminProductRequest req, CancellationToken ct = default);
    Task<ProductDetailDto?> UpdateProductAsync(int id, AdminProductRequest req, CancellationToken ct = default);
    Task<bool> DeleteProductAsync(int id, CancellationToken ct = default);
    Task<IList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDto?> CreateCategoryAsync(AdminCategoryRequest req, CancellationToken ct = default);
    Task<CategoryDto?> UpdateCategoryAsync(int id, AdminCategoryRequest req, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default);
    Task<IList<OrderHistoryDto>> GetOrdersAsync(CancellationToken ct = default);
    Task<OrderDto?> UpdateOrderStatusAsync(int id, string status, CancellationToken ct = default);
}

public class DomainException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
