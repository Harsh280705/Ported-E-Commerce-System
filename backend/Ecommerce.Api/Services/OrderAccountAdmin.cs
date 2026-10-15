using Ecommerce.Core;
using Ecommerce.Core.Dtos;
using Ecommerce.Core.Entities;
using Ecommerce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Services;

public class OrderService(AppDbContext db, ICartService carts) : IOrderService
{
    private static AddressDto Snap(AddressInput a, int? id) =>
        new(id ?? 0, a.FullName, a.Line1, a.Line2, a.City, a.State, a.PostalCode, a.Country, a.Phone, false);

    private static OrderDto Map(Order o) => new(o.Id, o.OrderNumber, o.OrderDate, o.Status, o.SubTotal,
        o.ShippingCost, o.TaxAmount, o.Total, o.ShippingMethod, o.PaymentMethod,
        new AddressDto(o.ShippingAddressId ?? 0, o.ShipFullName, o.ShipLine1, o.ShipLine2, o.ShipCity, o.ShipState, o.ShipPostalCode, o.ShipCountry, null, false),
        new AddressDto(o.BillingAddressId ?? 0, o.BillFullName, o.BillLine1, o.BillLine2, o.BillCity, o.BillState, o.BillPostalCode, o.BillCountry, null, false),
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.LineTotal)).ToList());

    public async Task<OrderDto> PlaceOrderAsync(int customerId, CheckoutRequest req, CancellationToken ct = default)
    {
        var ship = (req.ShippingMethod ?? "standard").ToLowerInvariant();
        if (!PricingRules.ShippingMethods.Contains(ship)) throw new DomainException("Invalid shipping method.", 400);
        var pay = (req.PaymentMethod ?? "cod").ToLowerInvariant();
        if (!PricingRules.PaymentMethods.Contains(pay)) throw new DomainException("Invalid payment method.", 400);
        ValidateAddress(req.ShippingAddress, "Shipping");
        ValidateAddress(req.BillingAddress, "Billing");

        var key = string.IsNullOrWhiteSpace(req.IdempotencyKey) ? Guid.NewGuid().ToString("N") : req.IdempotencyKey.Trim();
        var dup = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.IdempotencyKey == key, ct);
        if (dup != null) return Map(dup);

        using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Load customer cart
            var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
            if (cart == null || cart.Items.Count == 0) throw new DomainException("Your cart is empty.", 400);

            decimal subtotal = 0;
            var lines = new List<OrderItem>();
            foreach (var ci in cart.Items)
            {
                var p = await db.Products.FirstOrDefaultAsync(x => x.Id == ci.ProductId, ct);
                if (p == null || !p.IsActive) throw new DomainException("A product in your cart is no longer available.", 409);
                if (ci.Quantity > p.Stock) throw new DomainException($"Only {p.Stock} unit(s) of '{p.Name}' available.", 409);
                subtotal += p.Price * ci.Quantity;
                lines.Add(new OrderItem { ProductId = p.Id, ProductName = p.Name, Quantity = ci.Quantity, UnitPrice = p.Price, LineTotal = p.Price * ci.Quantity });
            }
            var shipping = PricingRules.ShippingFor(subtotal, ship);
            var tax = PricingRules.TaxFor(subtotal);
            var total = subtotal + shipping + tax;

            var order = new Order
            {
                OrderNumber = "NE-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Random.Shared.Next(100000, 999999),
                CustomerId = customerId, OrderDate = DateTime.UtcNow, Status = "Placed",
                SubTotal = subtotal, ShippingCost = shipping, TaxAmount = tax, Total = total,
                ShippingMethod = ship, PaymentMethod = pay,
                ShippingAddressId = req.ShippingAddressId, BillingAddressId = req.BillingAddressId,
                ShipFullName = req.ShippingAddress.FullName, ShipLine1 = req.ShippingAddress.Line1, ShipLine2 = req.ShippingAddress.Line2,
                ShipCity = req.ShippingAddress.City, ShipState = req.ShippingAddress.State,
                ShipPostalCode = req.ShippingAddress.PostalCode, ShipCountry = req.ShippingAddress.Country,
                BillFullName = req.BillingAddress.FullName, BillLine1 = req.BillingAddress.Line1, BillLine2 = req.BillingAddress.Line2,
                BillCity = req.BillingAddress.City, BillState = req.BillingAddress.State,
                BillPostalCode = req.BillingAddress.PostalCode, BillCountry = req.BillingAddress.Country,
                IdempotencyKey = key,
            };
            foreach (var l in lines) order.Items.Add(l);
            db.Orders.Add(order);

            foreach (var l in lines)
            {
                var p = await db.Products.FirstAsync(x => x.Id == l.ProductId, ct);
                p.Stock = Math.Max(0, p.Stock - l.Quantity);
            }
            await db.SaveChangesAsync(ct);
            db.CartItems.RemoveRange(cart.Items);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Map(order);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static void ValidateAddress(AddressInput a, string prefix)
    {
        if (string.IsNullOrWhiteSpace(a.FullName) || string.IsNullOrWhiteSpace(a.Line1) || string.IsNullOrWhiteSpace(a.City)
            || string.IsNullOrWhiteSpace(a.State) || string.IsNullOrWhiteSpace(a.PostalCode) || string.IsNullOrWhiteSpace(a.Country))
            throw new DomainException($"{prefix} address is incomplete.", 400);
    }

    public async Task<IList<OrderHistoryDto>> GetHistoryAsync(int customerId, bool isAdmin, CancellationToken ct = default)
    {
        var q = db.Orders.Include(o => o.Items).AsQueryable();
        if (!isAdmin) q = q.Where(o => o.CustomerId == customerId);
        return await q.OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderHistoryDto(o.Id, o.OrderNumber, o.OrderDate, o.Status, o.Total, o.Items.Sum(i => i.Quantity)))
            .ToListAsync(ct);
    }

    public async Task<OrderDto?> GetDetailAsync(int customerId, int id, bool isAdmin, CancellationToken ct = default)
    {
        var o = await db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (o == null) return null;
        if (!isAdmin && o.CustomerId != customerId) throw new DomainException("Forbidden.", 403);
        return Map(o);
    }
}

public class AccountService(AppDbContext db) : IAccountService
{
    public async Task<ProfileDto?> GetProfileAsync(int id, CancellationToken ct = default)
    {
        var c = await db.Customers.FindAsync([id], ct);
        return c == null ? null : new ProfileDto(c.Id, c.Email, c.FirstName, c.LastName, c.Role);
    }

    public async Task<ProfileDto?> UpdateProfileAsync(int id, UpdateProfileRequest req, CancellationToken ct = default)
    {
        var c = await db.Customers.FindAsync([id], ct);
        if (c == null) return null;
        c.FirstName = req.FirstName.Trim(); c.LastName = req.LastName.Trim();
        await db.SaveChangesAsync(ct);
        return new ProfileDto(c.Id, c.Email, c.FirstName, c.LastName, c.Role);
    }

    public async Task<IList<AddressDto>> GetAddressesAsync(int id, CancellationToken ct = default) =>
        await db.Addresses.Where(a => a.CustomerId == id).OrderByDescending(a => a.IsDefault)
            .Select(a => new AddressDto(a.Id, a.FullName, a.Line1, a.Line2, a.City, a.State, a.PostalCode, a.Country, a.Phone, a.IsDefault))
            .ToListAsync(ct);

    public async Task<AddressDto> CreateAddressAsync(int id, CreateAddressRequest req, CancellationToken ct = default)
    {
        if (req.IsDefault)
            foreach (var a in await db.Addresses.Where(x => x.CustomerId == id && x.IsDefault).ToListAsync(ct)) a.IsDefault = false;
        else if (!await db.Addresses.AnyAsync(x => x.CustomerId == id, ct)) req = req with { IsDefault = true };
        var e = new Address
        {
            CustomerId = id, FullName = req.FullName.Trim(), Line1 = req.Line1.Trim(), Line2 = req.Line2,
            City = req.City.Trim(), State = req.State.Trim(), PostalCode = req.PostalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(req.Country) ? "India" : req.Country.Trim(), Phone = req.Phone, IsDefault = req.IsDefault,
        };
        db.Addresses.Add(e);
        await db.SaveChangesAsync(ct);
        return new AddressDto(e.Id, e.FullName, e.Line1, e.Line2, e.City, e.State, e.PostalCode, e.Country, e.Phone, e.IsDefault);
    }

    public async Task<AddressDto?> UpdateAddressAsync(int customerId, int id, UpdateAddressRequest req, CancellationToken ct = default)
    {
        var e = await db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId, ct);
        if (e == null) return null;
        if (req.IsDefault)
            foreach (var a in await db.Addresses.Where(x => x.CustomerId == customerId && x.IsDefault && x.Id != id).ToListAsync(ct)) a.IsDefault = false;
        e.FullName = req.FullName.Trim(); e.Line1 = req.Line1.Trim(); e.Line2 = req.Line2;
        e.City = req.City.Trim(); e.State = req.State.Trim(); e.PostalCode = req.PostalCode.Trim();
        e.Country = req.Country.Trim(); e.Phone = req.Phone; e.IsDefault = req.IsDefault;
        await db.SaveChangesAsync(ct);
        return new AddressDto(e.Id, e.FullName, e.Line1, e.Line2, e.City, e.State, e.PostalCode, e.Country, e.Phone, e.IsDefault);
    }

    public async Task<bool> DeleteAddressAsync(int customerId, int id, CancellationToken ct = default)
    {
        var e = await db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId, ct);
        if (e == null) return false;
        var wasDefault = e.IsDefault;
        db.Addresses.Remove(e);
        await db.SaveChangesAsync(ct);
        if (wasDefault)
        {
            var next = await db.Addresses.FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);
            if (next != null) { next.IsDefault = true; await db.SaveChangesAsync(ct); }
        }
        return true;
    }
}

public class AdminService(AppDbContext db) : IAdminService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var totalProducts = await db.Products.CountAsync(ct);
        var totalOrders = await db.Orders.CountAsync(ct);
        var totalCustomers = await db.Customers.CountAsync(ct);
        var low = await db.Products.CountAsync(p => p.Stock > 0 && p.Stock < 10, ct);
        var oos = await db.Products.CountAsync(p => p.Stock <= 0, ct);
        var pending = await db.Orders.CountAsync(o => o.Status == "Placed", ct);
        var revenue = await db.Orders.Where(o => o.Status != "Cancelled" && o.Status != "Refunded").SumAsync(o => (decimal?)o.Total, ct) ?? 0;
        return new DashboardDto(totalProducts, totalOrders, totalCustomers, low, oos, pending, revenue);
    }

    public async Task<PagedResultDto<ProductCardDto>> GetProductsAsync(int page, int pageSize, int? categoryId, string? search, string? sort, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        if (sort == null || !PricingRules.ValidSorts.Contains(sort)) sort = "newest";
        var q = db.Products.Include(p => p.Category).AsQueryable();
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(s) || p.Description.ToLower().Contains(s) || p.Sku.ToLower().Contains(s));
        }
        q = sort switch
        {
            "price_asc" => q.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "price_desc" => q.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            "name_asc" => q.OrderBy(p => p.Name).ThenBy(p => p.Id),
            "name_desc" => q.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
            "recommended" => q.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
        };
        var total = await q.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        if (totalPages > 0 && page > totalPages) page = totalPages;
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResultDto<ProductCardDto>(items.Select(p => new ProductCardDto(p.Id, p.Name, p.Slug, p.Description,
            p.Price, p.CompareAtPrice, PricingRules.DiscountPct(p.Price, p.CompareAtPrice), p.Stock, p.Stock > 0, p.ImageUrl, p.CategoryId, p.Category?.Name ?? "")).ToList(),
            page, pageSize, total, totalPages);
    }

    private static string Slugify(string name) =>
        string.Join("-", System.Text.RegularExpressions.Regex.Split(name.ToLowerInvariant(), @"[^a-z0-9]+").Where(s => s.Length > 0));

    public async Task<ProductDetailDto?> CreateProductAsync(AdminProductRequest req, CancellationToken ct = default)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == req.CategoryId, ct)) throw new DomainException("Category not found.", 400);
        var slug = string.IsNullOrWhiteSpace(req.Slug) ? Slugify(req.Name) : Slugify(req.Slug);
        if (await db.Products.AnyAsync(p => p.Slug == slug, ct)) throw new DomainException("Slug already exists.", 409);
        if (req.Price < 0 || req.Stock < 0) throw new DomainException("Price and stock must be non-negative.", 400);
        var p = new Product
        {
            Name = req.Name.Trim(), Slug = slug, Description = req.Description ?? "", Price = req.Price,
            CompareAtPrice = req.CompareAtPrice, Stock = req.Stock,
            ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl) ? "/images/placeholder.png" : req.ImageUrl,
            CategoryId = req.CategoryId, IsActive = req.IsActive, IsFeatured = req.IsFeatured,
            Sku = "NE-" + Random.Shared.Next(10000, 99999),
        };
        db.Products.Add(p);
        await db.SaveChangesAsync(ct);
        return await GetDetailAsync(p.Id, ct);
    }

    private async Task<ProductDetailDto?> GetDetailAsync(int id, CancellationToken ct)
    {
        var p = await db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id, ct);
        return p == null ? null : new ProductDetailDto(p.Id, p.Name, p.Slug, p.Description, p.Price, p.CompareAtPrice,
            PricingRules.DiscountPct(p.Price, p.CompareAtPrice), p.Stock, p.Stock > 0, p.ImageUrl, p.CategoryId, p.Category?.Name ?? "");
    }

    public async Task<ProductDetailDto?> UpdateProductAsync(int id, AdminProductRequest req, CancellationToken ct = default)
    {
        var p = await db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null) return null;
        if (!await db.Categories.AnyAsync(c => c.Id == req.CategoryId, ct)) throw new DomainException("Category not found.", 400);
        var slug = string.IsNullOrWhiteSpace(req.Slug) ? p.Slug : Slugify(req.Slug);
        if (await db.Products.AnyAsync(x => x.Slug == slug && x.Id != id, ct)) throw new DomainException("Slug already exists.", 409);
        if (req.Price < 0 || req.Stock < 0) throw new DomainException("Price and stock must be non-negative.", 400);
        p.Name = req.Name.Trim(); p.Slug = slug; p.Description = req.Description ?? ""; p.Price = req.Price;
        p.CompareAtPrice = req.CompareAtPrice; p.Stock = req.Stock;
        p.ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl) ? p.ImageUrl : req.ImageUrl;
        p.CategoryId = req.CategoryId; p.IsActive = req.IsActive; p.IsFeatured = req.IsFeatured;
        await db.SaveChangesAsync(ct);
        return await GetDetailAsync(id, ct);
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken ct = default)
    {
        if (await db.OrderItems.AnyAsync(i => i.ProductId == id, ct))
            throw new DomainException("Product has historical orders and cannot be deleted. Deactivate it instead.", 409);
        if (await db.CartItems.AnyAsync(i => i.ProductId == id, ct))
            throw new DomainException("Product is present in carts and cannot be deleted now.", 409);
        var p = await db.Products.FindAsync([id], ct);
        if (p == null) return false;
        db.Products.Remove(p);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default) =>
        await db.Categories.OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, c.Products.Count)).ToListAsync(ct);

    public async Task<CategoryDto?> CreateCategoryAsync(AdminCategoryRequest req, CancellationToken ct = default)
    {
        var slug = Slugify(string.IsNullOrWhiteSpace(req.Slug) ? req.Name : req.Slug);
        if (await db.Categories.AnyAsync(c => c.Slug == slug, ct)) throw new DomainException("Slug already exists.", 409);
        var c = new Category { Name = req.Name.Trim(), Slug = slug, Description = req.Description ?? "", ImageUrl = req.ImageUrl ?? "/images/placeholder.png", IsActive = req.IsActive, DisplayOrder = req.DisplayOrder };
        db.Categories.Add(c);
        await db.SaveChangesAsync(ct);
        return new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, 0);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int id, AdminCategoryRequest req, CancellationToken ct = default)
    {
        var c = await db.Categories.FindAsync([id], ct);
        if (c == null) return null;
        var slug = Slugify(string.IsNullOrWhiteSpace(req.Slug) ? req.Name : req.Slug);
        if (await db.Categories.AnyAsync(x => x.Slug == slug && x.Id != id, ct)) throw new DomainException("Slug already exists.", 409);
        c.Name = req.Name.Trim(); c.Slug = slug; c.Description = req.Description ?? "";
        c.ImageUrl = req.ImageUrl ?? c.ImageUrl; c.IsActive = req.IsActive; c.DisplayOrder = req.DisplayOrder;
        await db.SaveChangesAsync(ct);
        var count = await db.Products.CountAsync(p => p.CategoryId == id, ct);
        return new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, count);
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(p => p.CategoryId == id, ct))
            throw new DomainException("Category contains products and cannot be deleted.", 409);
        var c = await db.Categories.FindAsync([id], ct);
        if (c == null) return false;
        db.Categories.Remove(c);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IList<OrderHistoryDto>> GetOrdersAsync(CancellationToken ct = default) =>
        await db.Orders.Include(o => o.Items).OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderHistoryDto(o.Id, o.OrderNumber, o.OrderDate, o.Status, o.Total, o.Items.Sum(i => i.Quantity))).ToListAsync(ct);

    public async Task<OrderDto?> UpdateOrderStatusAsync(int id, string status, CancellationToken ct = default)
    {
        if (!PricingRules.ValidStatuses.Contains(status)) throw new DomainException("Invalid status.", 400);
        var o = await db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (o == null) return null;
        if (!PricingRules.IsValidTransition(o.Status, status)) throw new DomainException($"Cannot transition from {o.Status} to {status}.", 409);
        o.Status = status;
        await db.SaveChangesAsync(ct);
        var svc = new OrderService(db, null!);
        return await svc.GetDetailAsync(o.CustomerId, o.Id, true, ct);
    }
}
