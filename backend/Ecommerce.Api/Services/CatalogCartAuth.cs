using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Core;
using Ecommerce.Core.Dtos;
using Ecommerce.Core.Entities;
using Ecommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Api.Services;

public class TokenService(IConfiguration cfg) : ITokenService
{
    private string Key()
    {
        var v = cfg["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(v) || v.StartsWith("${"))
            v = Environment.GetEnvironmentVariable("JWT_KEY");
        if (string.IsNullOrWhiteSpace(v) || v.StartsWith("${"))
            v = "dev-only-signing-key-min-32-chars-0123456789abcdef";
        return v;
    }

    private string Issuer() => Value("Jwt:Issuer", "JWT_ISSUER", "ecommerce-api");
    private string Audience() => Value("Jwt:Audience", "JWT_AUDIENCE", "ecommerce-spa");

    private string Value(string key, string env, string fallback)
    {
        var v = cfg[key] ?? Environment.GetEnvironmentVariable(env);
        return string.IsNullOrWhiteSpace(v) || v.StartsWith("${") ? fallback : v;
    }

    public string CreateAccessToken(Customer c)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, c.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, c.Email),
            new Claim(ClaimTypes.NameIdentifier, c.Id.ToString()),
            new Claim(ClaimTypes.Email, c.Email),
            new Claim(ClaimTypes.Role, c.Role),
            new Claim("role", c.Role),
        };
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(cfg["Jwt:AccessTokenMinutes"] ?? "15"));
        var tok = new JwtSecurityToken(Issuer(), Audience(), claims, expires: expires, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(tok);
    }

    public string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public class AuthService(AppDbContext db, ITokenService tokens, IConfiguration cfg) : IAuthService
{
    private readonly PasswordHasher<Customer> _hasher = new();

    private AuthResponse ToAuth(Customer c)
    {
        var access = tokens.CreateAccessToken(c);
        return new AuthResponse(access, DateTime.UtcNow.AddMinutes(int.Parse(cfg["Jwt:AccessTokenMinutes"] ?? "15")),
            new UserDto(c.Id, c.Email, c.FirstName, c.LastName, c.Role));
    }

    private async Task<(string Raw, DateTime Expires)> IssueRefreshAsync(int customerId, CancellationToken ct)
    {
        var raw = tokens.GenerateRefreshToken();
        var days = int.Parse(cfg["Jwt:RefreshTokenDays"] ?? "7");
        var exp = DateTime.UtcNow.AddDays(days);
        db.RefreshTokens.Add(new RefreshToken { CustomerId = customerId, TokenHash = tokens.HashToken(raw), ExpiresAt = exp });
        await db.SaveChangesAsync(ct);
        return (raw, exp);
    }

    public async Task<(AuthResponse Auth, string RefreshToken, DateTime RefreshExpires)> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Customers.AnyAsync(c => c.Email == email, ct))
            throw new DomainException("Email is already registered.", 409);
        var c = new Customer { Email = email, FirstName = req.FirstName.Trim(), LastName = req.LastName.Trim(), Role = "Customer" };
        c.PasswordHash = _hasher.HashPassword(c, req.Password);
        db.Customers.Add(c);
        await db.SaveChangesAsync(ct);
        var (raw, exp) = await IssueRefreshAsync(c.Id, ct);
        return (ToAuth(c), raw, exp);
    }

    public async Task<(AuthResponse Auth, string RefreshToken, DateTime RefreshExpires)> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var c = await db.Customers.FirstOrDefaultAsync(x => x.Email == email, ct);
        if (c == null || _hasher.VerifyHashedPassword(c, c.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
            throw new DomainException("Invalid email or password.", 401);
        var (raw, exp) = await IssueRefreshAsync(c.Id, ct);
        return (ToAuth(c), raw, exp);
    }

    public async Task<(AuthResponse Auth, string RefreshToken, DateTime RefreshExpires)?> RefreshAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) throw new DomainException("Refresh token is required.", 401);
        var hash = tokens.HashToken(refreshToken);
        var stored = await db.RefreshTokens.Include(r => r.Customer).FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        if (stored == null || !stored.IsActive || stored.Customer == null)
            throw new DomainException("Invalid or expired refresh token.", 401);
        // Rotation: revoke old, issue new (detect reuse via revoked lookup)
        stored.RevokedAt = DateTime.UtcNow;
        var raw = tokens.GenerateRefreshToken();
        var days = int.Parse(cfg["Jwt:RefreshTokenDays"] ?? "7");
        var exp = DateTime.UtcNow.AddDays(days);
        var replacement = new RefreshToken { CustomerId = stored.CustomerId, TokenHash = tokens.HashToken(raw), ExpiresAt = exp };
        stored.ReplacedByHash = replacement.TokenHash;
        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(ct);
        return (ToAuth(stored.Customer), raw, exp);
    }

    public async Task RevokeAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = tokens.HashToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        if (stored != null && !stored.IsRevoked) { stored.RevokedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
    }

    public async Task RevokeAllAsync(int customerId, CancellationToken ct = default)
    {
        var act = await db.RefreshTokens.Where(r => r.CustomerId == customerId && r.RevokedAt == null).ToListAsync(ct);
        foreach (var r in act) r.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

public class CatalogService(AppDbContext db) : ICatalogService
{
    private static ProductCardDto Card(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Price, p.CompareAtPrice,
        PricingRules.DiscountPct(p.Price, p.CompareAtPrice), p.Stock, p.Stock > 0,
        string.IsNullOrWhiteSpace(p.ImageUrl) ? "/images/placeholder.png" : p.ImageUrl,
        p.CategoryId, p.Category?.Name ?? "");

    public async Task<IList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var cats = await db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(ct);
        var counts = await db.Products.Where(p => p.IsActive).GroupBy(p => p.CategoryId).ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
        return cats.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, counts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<PagedResultDto<ProductCardDto>> SearchProductsAsync(int page, int pageSize, int? categoryId, string? search, string? sort, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is < 1 or > 60 ? 12 : pageSize;
        if (sort == null || !PricingRules.ValidSorts.Contains(sort)) sort = "recommended";
        var q = db.Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();
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
            "newest" => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
        };
        var total = await q.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        if (totalPages > 0 && page > totalPages) page = totalPages;
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResultDto<ProductCardDto>(items.Select(Card).ToList(), page, pageSize, total, totalPages);
    }

    public async Task<ProductDetailDto?> GetProductAsync(int id, CancellationToken ct = default)
    {
        var p = await db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (p == null) return null;
        return new ProductDetailDto(p.Id, p.Name, p.Slug, p.Description, p.Price, p.CompareAtPrice,
            PricingRules.DiscountPct(p.Price, p.CompareAtPrice), p.Stock, p.Stock > 0,
            string.IsNullOrWhiteSpace(p.ImageUrl) ? "/images/placeholder.png" : p.ImageUrl, p.CategoryId, p.Category?.Name ?? "");
    }
}

public class CartService(AppDbContext db) : ICartService
{
    private async Task<Cart> GetOrCreateAsync(int? customerId, string? anon, CancellationToken ct)
    {
        Cart? cart = null;
        if (customerId.HasValue) cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        if (cart == null && !string.IsNullOrWhiteSpace(anon))
            cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(c => c.AnonymousId == anon, ct);
        if (cart == null)
        {
            cart = new Cart { CustomerId = customerId, AnonymousId = customerId.HasValue ? null : anon };
            db.Carts.Add(cart);
            await db.SaveChangesAsync(ct);
            cart.Items = new List<CartItem>();
        }
        return cart;
    }

    private static CartDto ToDto(Cart cart)
    {
        var lines = cart.Items.Select(i => new CartItemDto(i.ProductId, i.Product?.Name ?? "Product",
            i.Product?.Slug ?? i.ProductId.ToString(),
            string.IsNullOrWhiteSpace(i.Product?.ImageUrl) ? "/images/placeholder.png" : i.Product!.ImageUrl,
            i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity, i.Product?.Stock ?? 0)).ToList();
        var sub = lines.Sum(l => l.LineTotal);
        var ship = PricingRules.ShippingFor(sub, "standard");
        var tax = PricingRules.TaxFor(sub);
        return new CartDto(lines, sub, ship, tax, sub + ship + tax, lines.Sum(l => l.Quantity));
    }

    public async Task<CartDto> GetCartAsync(int? customerId, string? anonymousId, CancellationToken ct = default)
    {
        Cart? cart = null;
        if (customerId.HasValue) cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        cart ??= !string.IsNullOrWhiteSpace(anonymousId)
            ? await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(c => c.AnonymousId == anonymousId, ct)
            : null;
        if (cart == null) return new CartDto([], 0, 0, 0, 0, 0);
        return ToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(int? customerId, string? anonymousId, int productId, int quantity, CancellationToken ct = default)
    {
        if (quantity < 1) quantity = 1;
        if (quantity > 99) throw new DomainException("Maximum 99 units per add.", 400);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, ct)
            ?? throw new DomainException("Product not found.", 404);
        if (product.Stock <= 0) throw new DomainException("This item is out of stock.", 409);
        var cart = await GetOrCreateAsync(customerId, anonymousId, ct);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        var newQty = quantity + (existing?.Quantity ?? 0);
        if (newQty > product.Stock) throw new DomainException($"Only {product.Stock} unit(s) available.", 409);
        if (existing != null) { existing.Quantity = newQty; existing.UnitPrice = product.Price; }
        else cart.Items.Add(new CartItem { CartId = cart.Id, ProductId = productId, Quantity = quantity, UnitPrice = product.Price });
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetCartAsync(customerId, anonymousId, ct);
    }

    public async Task<CartDto> UpdateItemAsync(int? customerId, string? anonymousId, int productId, int quantity, CancellationToken ct = default)
    {
        var cart = await GetOrCreateAsync(customerId, anonymousId, ct);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId) ?? throw new DomainException("Cart item not found.", 404);
        if (quantity <= 0) { db.CartItems.Remove(item); await db.SaveChangesAsync(ct); return await GetCartAsync(customerId, anonymousId, ct); }
        if (quantity > 99) throw new DomainException("Maximum 99 units.", 400);
        var product = await db.Products.FirstAsync(p => p.Id == productId, ct);
        if (quantity > product.Stock) throw new DomainException($"Only {product.Stock} unit(s) available.", 409);
        item.Quantity = quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetCartAsync(customerId, anonymousId, ct);
    }

    public async Task<CartDto> RemoveItemAsync(int? customerId, string? anonymousId, int productId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateAsync(customerId, anonymousId, ct);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId) ?? throw new DomainException("Cart item not found.", 404);
        db.CartItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return await GetCartAsync(customerId, anonymousId, ct);
    }

    public async Task ClearCartAsync(int? customerId, string? anonymousId, CancellationToken ct = default)
    {
        Cart? cart = null;
        if (customerId.HasValue) cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        cart ??= !string.IsNullOrWhiteSpace(anonymousId)
            ? await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.AnonymousId == anonymousId, ct) : null;
        if (cart == null) return;
        db.CartItems.RemoveRange(cart.Items);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CartDto> MergeAsync(int customerId, string? anonymousId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(anonymousId)) return await GetCartAsync(customerId, null, ct);
        var guest = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.AnonymousId == anonymousId, ct);
        if (guest == null || guest.Items.Count == 0) return await GetCartAsync(customerId, null, ct);
        var mine = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        if (mine == null) { guest.CustomerId = customerId; guest.AnonymousId = null; await db.SaveChangesAsync(ct); return await GetCartAsync(customerId, null, ct); }
        foreach (var g in guest.Items)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == g.ProductId, ct);
            if (product == null || !product.IsActive) continue;
            var ex = mine.Items.FirstOrDefault(i => i.ProductId == g.ProductId);
            var want = g.Quantity + (ex?.Quantity ?? 0);
            var capped = Math.Min(want, Math.Min(product.Stock, 99));
            if (capped <= 0) continue;
            if (ex != null) { ex.Quantity = capped; ex.UnitPrice = product.Price; }
            else mine.Items.Add(new CartItem { CartId = mine.Id, ProductId = g.ProductId, Quantity = capped, UnitPrice = product.Price });
        }
        db.Carts.Remove(guest);
        await db.SaveChangesAsync(ct);
        return await GetCartAsync(customerId, null, ct);
    }
}
