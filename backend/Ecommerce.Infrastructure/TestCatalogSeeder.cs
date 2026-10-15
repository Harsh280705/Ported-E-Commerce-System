using Ecommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure;

/// <summary>
/// Development/test catalog seeder. Generates a deterministic set of test products
/// (default 2000) across the existing categories. Safe to rerun: products are keyed
/// by stable "tst-" slugs and existing rows are never modified or deleted.
/// Enable explicitly via configuration <c>SeedTestCatalog=true</c> (never automatic).
/// </summary>
public static class TestCatalogSeeder
{
    public const string SlugPrefix = "tst-";
    public const int DefaultTarget = 2000;

    private sealed record CatSpec(string Slug, string Label, string[] Descriptors, string[] Materials, string[] Types, decimal MinPrice, decimal MaxPrice);

    private static readonly CatSpec[] Specs =
    [
        new("fashion", "Fashion",
            ["Classic", "Slim-Fit", "Festive", "Casual", "Premium", "Heritage", "Urban", "Royal"],
            ["Cotton", "Silk", "Denim", "Linen", "Wool", "Georgette"],
            ["Kurta", "Formal Shirt", "Jeans", "T-Shirt", "Saree", "Jacket", "Trousers", "Blazer"],
            499, 4999),
        new("footwear", "Footwear",
            ["Pro", "AirFlex", "Trail", "Street", "Classic", "Endura", "CloudWalk", "Metro"],
            ["Leather", "Canvas", "Mesh", "Suede", "Synthetic", "Knit"],
            ["Running Shoes", "Sneakers", "Derby Shoes", "Hiking Boots", "Sandals", "Loafers"],
            799, 6999),
        new("home-kitchen", "Home & Kitchen",
            ["Non-Stick", "Ceramic", "Royal", "Compact", "Deluxe", "Everyday", "Artisan", "Prime"],
            ["Steel", "Ceramic", "Cotton", "Bamboo", "Glass", "Cast-Iron"],
            ["Cookware Set", "Dinner Set", "Bedsheet", "Table Lamp", "Pressure Cooker", "Kettle", "Cushion Cover", "Wall Clock"],
            349, 5499),
        new("electronics", "Electronics",
            ["Wireless", "Pro", "Ultra", "Smart", "Turbo", "Nova", "Prime", "Echo"],
            ["Matte Black", "Silver", "Graphite", "White", "Navy", "Charcoal"],
            ["Headphones", "Speaker", "Charger", "Smart Watch", "Earbuds", "Power Bank", "Keyboard", "Mouse"],
            799, 12999),
        new("beauty", "Beauty",
            ["Herbal", "Matte", "Repair", "Waterproof", "Glow", "Pure", "Silk", "Fresh"],
            ["Aloe", "Argan", "Rose", "Charcoal", "Vitamin-E", "Neem"],
            ["Face Wash", "Lipstick Set", "Hair Oil", "Kajal", "Moisturizer", "Shampoo", "Serum", "Compact"],
            149, 1499),
        new("sports", "Sports",
            ["Pro", "Flex", "Tournament", "Training", "Endurance", "Club", "Aero", "Power"],
            ["Carbon", "Foam", "Willow", "Rubber", "Nylon", "Alloy"],
            ["Yoga Mat", "Dumbbell Set", "Cricket Bat", "Football", "Badminton Racket", "Gym Bag", "Skipping Rope", "Tennis Balls"],
            249, 5999),
        new("books", "Books",
            ["Bestseller", "Illustrated", "Complete", "Essential", "Classic", "Junior", "Scholar", "Pocket"],
            ["Paperback", "Hardcover", "Deluxe", "Boxed", "Large-Print", "Collector"],
            ["Novel Pack", "Story Collection", "Exam Guide", "Cookbook", "Biography", "Poetry Volume", "Atlas", "Grammar Book"],
            199, 1299),
    ];

    public sealed record SeedResult(int Inserted, int TotalProducts, int Categories, int ImagesNormalized);

    public static async Task<SeedResult> EnsureTestCatalogAsync(IServiceProvider sp, int targetCount = DefaultTarget, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsSqlServer())
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct); // test providers (SQLite)

        var normalized = await NormalizeLegacyImageUrlsAsync(db, ct);

        var cats = await db.Categories.ToListAsync(ct);
        if (cats.Count == 0)
            throw new InvalidOperationException("Seed base categories first (SeedData) before generating the test catalog.");

        var existing = await db.Products.Where(p => p.Slug.StartsWith(SlugPrefix)).Select(p => p.Slug).ToListAsync(ct);
        var have = new HashSet<string>(existing);
        var need = Math.Max(0, targetCount - have.Count);

        var inserted = 0;
        if (need > 0)
        {
            var batch = new List<Product>(500);
            var n = have.Count; // continue numbering after existing test rows
            var guard = 0;
            while (inserted < need && guard++ < targetCount + 5000)
            {
                n++;
                var spec = Specs[(n - 1) % Specs.Length];
                var cat = cats.FirstOrDefault(c => c.Slug == spec.Slug) ?? cats[(n - 1) % cats.Count];
                var slug = $"{SlugPrefix}{cat.Slug}-{n:0000}";
                if (have.Contains(slug)) continue;

                var combo = n - 1;
                var desc = spec.Descriptors[combo % spec.Descriptors.Length];
                var mat = spec.Materials[(combo / spec.Descriptors.Length) % spec.Materials.Length];
                var type = spec.Types[(combo / (spec.Descriptors.Length * spec.Materials.Length)) % spec.Types.Length];
                var name = $"{desc} {mat} {type} {n:0000}";

                var span = (double)(spec.MaxPrice - spec.MinPrice);
                var price = Math.Round(spec.MinPrice + (decimal)((n * 7919) % Math.Max(1, (int)span)) + ((n * 13) % 100) / 100m, 2);
                decimal? compare = n % 4 == 0 ? Math.Round(price * (1.15m + ((n * 7) % 16) / 100m), 2) : null;
                var stock = n % 20 == 0 ? 0 : ((n * 53) % 180) + 1;

                batch.Add(new Product
                {
                    Sku = $"TST-{n:00000}",
                    Name = name,
                    Slug = slug,
                    Description = $"{name}: {mat.ToLowerInvariant()} {type.ToLowerInvariant()} for {spec.Label.ToLowerInvariant()} shoppers. Deterministic test-catalog entry with category-accurate imagery.",
                    Price = price,
                    CompareAtPrice = compare,
                    Stock = stock,
                    ImageUrl = $"/images/photos/{slug}.webp",
                    CategoryId = cat.Id,
                    IsActive = true,
                    IsFeatured = n % 25 == 0,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-n),
                });
                have.Add(slug);
                inserted++;

                if (batch.Count >= 500)
                {
                    db.Products.AddRange(batch);
                    await db.SaveChangesAsync(ct);
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
            {
                db.Products.AddRange(batch);
                await db.SaveChangesAsync(ct);
            }
        }

        var total = await db.Products.CountAsync(ct);
        return new SeedResult(inserted, total, cats.Count, normalized);
    }

    /// <summary>
    /// Rewrites unstable external image URLs (picsum random photos) and legacy
    /// generated placeholder art (/images/products/*.svg) to the deterministic
    /// per-product photograph (/images/photos/&lt;slug&gt;.webp). Only touches rows
    /// whose URL matches those known placeholder patterns; admin-set custom URLs
    /// are never modified.
    /// </summary>
    public static async Task<int> NormalizeLegacyImageUrlsAsync(AppDbContext db, CancellationToken ct = default)
    {
        var rows = await db.Products.Where(p => p.ImageUrl.Contains("picsum.photos") || p.ImageUrl.StartsWith("/images/products/")).ToListAsync(ct);
        foreach (var p in rows)
            p.ImageUrl = $"/images/photos/{p.Slug}.webp";
        if (rows.Count > 0)
            await db.SaveChangesAsync(ct);
        return rows.Count;
    }
}
