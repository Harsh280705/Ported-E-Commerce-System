namespace Ecommerce.Core.Dtos;

// Auth
public record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string? RefreshToken);
public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);
public record UserDto(int Id, string Email, string FirstName, string LastName, string Role);

// Catalog
public record CategoryDto(int Id, string Name, string Slug, string Description, string ImageUrl, int ProductCount);
public record ProductCardDto(int Id, string Name, string Slug, string Description, decimal Price, decimal? CompareAtPrice, int? DiscountPct, int Stock, bool InStock, string ImageUrl, int CategoryId, string CategoryName);
public record ProductDetailDto(int Id, string Name, string Slug, string Description, decimal Price, decimal? CompareAtPrice, int? DiscountPct, int Stock, bool InStock, string ImageUrl, int CategoryId, string CategoryName);
public record PagedResultDto<T>(IList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

// Cart
public record CartItemDto(int ProductId, string ProductName, string Slug, string ImageUrl, int Quantity, decimal UnitPrice, decimal LineTotal, int StockAvailable);
public record CartDto(IList<CartItemDto> Items, decimal SubTotal, decimal Shipping, decimal Tax, decimal GrandTotal, int ItemCount);
public record AddCartItemRequest(int ProductId, int Quantity);
public record UpdateCartItemRequest(int Quantity);

// Orders
public record AddressInput(string FullName, string Line1, string? Line2, string City, string State, string PostalCode, string Country, string? Phone);
public record CheckoutRequest(AddressInput ShippingAddress, AddressInput BillingAddress, int? ShippingAddressId, int? BillingAddressId, string ShippingMethod, string PaymentMethod, string? IdempotencyKey);
public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
public record OrderDto(int Id, string OrderNumber, DateTime OrderDate, string Status, decimal SubTotal, decimal ShippingCost, decimal TaxAmount, decimal Total, string ShippingMethod, string PaymentMethod, AddressDto ShippingAddress, AddressDto BillingAddress, IList<OrderItemDto> Items);
public record OrderHistoryDto(int Id, string OrderNumber, DateTime OrderDate, string Status, decimal Total, int ItemCount);

// Account
public record ProfileDto(int Id, string Email, string FirstName, string LastName, string Role);
public record UpdateProfileRequest(string FirstName, string LastName);
public record AddressDto(int Id, string FullName, string Line1, string? Line2, string City, string State, string PostalCode, string Country, string? Phone, bool IsDefault);
public record CreateAddressRequest(string FullName, string Line1, string? Line2, string City, string State, string PostalCode, string Country, string? Phone, bool IsDefault);
public record UpdateAddressRequest(string FullName, string Line1, string? Line2, string City, string State, string PostalCode, string Country, string? Phone, bool IsDefault);

// Admin
public record AdminProductRequest(string Name, string Slug, string Description, decimal Price, decimal? CompareAtPrice, int Stock, string ImageUrl, int CategoryId, bool IsActive, bool IsFeatured);
public record AdminCategoryRequest(string Name, string Slug, string Description, string ImageUrl, bool IsActive, int DisplayOrder);
public record UpdateOrderStatusRequest(string Status);
public record DashboardDto(int TotalProducts, int TotalOrders, int TotalCustomers, int LowStockProducts, int OutOfStockProducts, int PendingOrders, decimal TotalRevenue);
