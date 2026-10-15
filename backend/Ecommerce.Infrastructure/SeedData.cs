using Ecommerce.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure;

public static class SeedData
{
    public static async Task EnsureSeededAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);

        if (!await db.Categories.AnyAsync(ct))
        {
            // Category art reuses genuine, category-matching product photographs
            // from frontend/public/images/photos/*.webp (served by nginx). These
            // files are verified to exist; the previous /images/*.png paths had
            // no corresponding static asset, so nginx returned index.html
            // (200 text/html) and the storefront fell back to "No image".
            var cats = new[]
            {
                new Category { Name = "Fashion", Slug = "fashion", Description = "Clothing for the family", ImageUrl = "/images/photos/banarasi-silk-saree.webp", DisplayOrder = 1 },
                new Category { Name = "Footwear", Slug = "footwear", Description = "Shoes for every occasion", ImageUrl = "/images/photos/running-shoes-pro.webp", DisplayOrder = 2 },
                new Category { Name = "Home & Kitchen", Slug = "home-kitchen", Description = "Home essentials", ImageUrl = "/images/photos/non-stick-cookware-set.webp", DisplayOrder = 3 },
                new Category { Name = "Electronics", Slug = "electronics", Description = "Audio and accessories", ImageUrl = "/images/photos/bluetooth-speaker.webp", DisplayOrder = 4 },
                new Category { Name = "Beauty", Slug = "beauty", Description = "Everyday beauty", ImageUrl = "/images/photos/herbal-face-wash.webp", DisplayOrder = 5 },
                new Category { Name = "Sports", Slug = "sports", Description = "Fitness and outdoor", ImageUrl = "/images/photos/cricket-bat-english.webp", DisplayOrder = 6 },
                new Category { Name = "Books", Slug = "books", Description = "Fiction, study and kids", ImageUrl = "/images/photos/fiction-bestseller-pack.webp", DisplayOrder = 7 },
            };
            db.Categories.AddRange(cats);
            await db.SaveChangesAsync(ct);
        }

        // Repair pre-existing databases whose categories still reference the
        // legacy /images/*.png paths (no static asset exists for them).
        // Only touches rows with those known-broken values; admin-customized
        // image URLs are never modified.
        await NormalizeLegacyCategoryImageUrlsAsync(db, ct);

        if (!await db.Products.AnyAsync(ct))
        {
            var cats = await db.Categories.ToListAsync(ct);
            Category By(string slug) => cats.First(c => c.Slug == slug);
            var products = new (string Name, string Slug, string Cat, decimal Price, decimal? Compare, int Stock, bool Featured)[]
            {
                ("Classic Cotton Kurta", "classic-cotton-kurta", "fashion", 1335.86m, null, 51, false),
                ("Slim-Fit Formal Shirt", "slim-fit-formal-shirt", "fashion", 1524.25m, null, 50, false),
                ("Indigo Denim Jeans", "indigo-denim-jeans", "fashion", 4725.76m, 5907.20m, 121, false),
                ("Cotton Casual T-Shirt", "cotton-casual-t-shirt", "fashion", 1403.54m, null, 99, false),
                ("Festive Silk Kurta", "festive-silk-kurta", "fashion", 3074.25m, null, 50, true),
                ("Banarasi Silk Saree", "banarasi-silk-saree", "fashion", 3353.54m, 4191.93m, 79, true),
                ("Running Shoes Pro", "running-shoes-pro", "footwear", 2999m, 3999m, 80, true),
                ("Leather Derby Shoes", "leather-derby-shoes", "footwear", 4499m, null, 45, false),
                ("Canvas Sneakers", "canvas-sneakers", "footwear", 1499m, 1999m, 120, false),
                ("Trail Hiking Boots", "trail-hiking-boots", "footwear", 5999m, null, 30, false),
                ("Non-Stick Cookware Set", "non-stick-cookware-set", "home-kitchen", 3499m, 4999m, 60, true),
                ("Ceramic Dinner Set", "ceramic-dinner-set", "home-kitchen", 2799m, null, 40, false),
                ("Cotton Bedsheet King", "cotton-bedsheet-king", "home-kitchen", 1299m, 1799m, 90, false),
                ("LED Table Lamp", "led-table-lamp", "home-kitchen", 899m, null, 110, false),
                ("Pressure Cooker 5L", "pressure-cooker-5l", "home-kitchen", 2199m, 2799m, 70, false),
                ("Wireless Headphones", "wireless-headphones", "electronics", 2499m, 3499m, 85, true),
                ("Bluetooth Speaker", "bluetooth-speaker", "electronics", 1999m, null, 95, false),
                ("Phone Charger 65W", "phone-charger-65w", "electronics", 999m, 1499m, 200, false),
                ("Smart Watch Fit", "smart-watch-fit", "electronics", 3999m, 5999m, 55, true),
                ("USB-C Earbuds", "usb-c-earbuds", "electronics", 1299m, null, 150, false),
                ("Herbal Face Wash", "herbal-face-wash", "beauty", 349m, 449m, 180, false),
                ("Matte Lipstick Set", "matte-lipstick-set", "beauty", 799m, null, 130, false),
                ("Hair Oil Repair", "hair-oil-repair", "beauty", 499m, 649m, 160, false),
                ("Kajal Waterproof", "kajal-waterproof", "beauty", 249m, null, 220, false),
                ("Yoga Mat Pro", "yoga-mat-pro", "sports", 1199m, 1599m, 75, false),
                ("Dumbbell Set 20kg", "dumbbell-set-20kg", "sports", 4999m, null, 25, false),
                ("Cricket Bat English", "cricket-bat-english", "sports", 3299m, 4299m, 35, true),
                ("Fiction Bestseller Pack", "fiction-bestseller-pack", "books", 899m, 1299m, 140, false),
                ("Kids Story Collection", "kids-story-collection", "books", 599m, null, 170, false),
                ("Exam Prep Guide", "exam-prep-guide", "books", 749m, 999m, 110, false),
            };
            int i = 1;
            foreach (var (name, slug, cat, price, compare, stock, feat) in products)
            {
                var c = By(cat);
                db.Products.Add(new Product
                {
                    Sku = $"DEMO-{i:00000}", Name = name, Slug = slug,
                    Description = $"Demo product '{name}' in category {c.Name}. Ported from the legacy catalogue.",
                    Price = price, CompareAtPrice = compare, Stock = stock,
                    ImageUrl = $"/images/photos/{slug}.webp",
                    CategoryId = c.Id, IsFeatured = feat, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-i),
                });
                i++;
            }
            await db.SaveChangesAsync(ct);
        }

        var hasher = new PasswordHasher<Customer>();
        if (!await db.Customers.AnyAsync(c => c.Email == "admin@yantra.local", ct))
        {
            var admin = new Customer { Email = "admin@yantra.local", FirstName = "Store", LastName = "Admin", Role = "Admin" };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin!123");
            db.Customers.Add(admin);
            await db.SaveChangesAsync(ct);
        }
        if (!await db.Customers.AnyAsync(c => c.Email == "customer@example.com", ct))
        {
            var cust = new Customer { Email = "customer@example.com", FirstName = "Demo", LastName = "Customer", Role = "Customer" };
            cust.PasswordHash = hasher.HashPassword(cust, "Customer!123");
            db.Customers.Add(cust);
            await db.SaveChangesAsync(ct);
            db.Addresses.Add(new Address
            {
                CustomerId = cust.Id, FullName = "Demo Customer", Line1 = "42 MG Road",
                City = "Bengaluru", State = "Karnataka", PostalCode = "560001", Country = "India",
                Phone = "9876543210", IsDefault = true,
            });
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Repairs category rows that still reference the legacy /images/*.png
    /// paths, for which no static asset exists. Only rows whose ImageUrl is
    /// exactly one of those known-broken values are updated; any customized
    /// URL (including null/empty handling elsewhere) is left untouched.
    /// </summary>
    public static async Task<int> NormalizeLegacyCategoryImageUrlsAsync(AppDbContext db, CancellationToken ct = default)
    {
        var fixedBySlug = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["fashion"] = "/images/photos/banarasi-silk-saree.webp",
            ["footwear"] = "/images/photos/running-shoes-pro.webp",
            ["home-kitchen"] = "/images/photos/non-stick-cookware-set.webp",
            ["electronics"] = "/images/photos/bluetooth-speaker.webp",
            ["beauty"] = "/images/photos/herbal-face-wash.webp",
            ["sports"] = "/images/photos/cricket-bat-english.webp",
            ["books"] = "/images/photos/fiction-bestseller-pack.webp",
        };
        var legacyBroken = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/images/fashion.png",
            "/images/footwear.png",
            "/images/home.png",
            "/images/tech.png",
            "/images/beauty.png",
            "/images/sports.png",
            "/images/books.png",
        };
        var rows = await db.Categories.ToListAsync(ct);
        var touched = 0;
        foreach (var c in rows)
        {
            if (legacyBroken.Contains(c.ImageUrl ?? string.Empty)
                && fixedBySlug.TryGetValue(c.Slug ?? string.Empty, out var fixedUrl))
            {
                c.ImageUrl = fixedUrl;
                touched++;
            }
        }
        if (touched > 0)
            await db.SaveChangesAsync(ct);
        return touched;
    }
}
