using Ecommerce.Core;
using FluentAssertions;

namespace Ecommerce.Tests.Unit;

public class PricingTests
{
    [Theory]
    [InlineData(0, "standard", 0)]
    [InlineData(10, "standard", 5.99)]
    [InlineData(74.99, "standard", 5.99)]
    [InlineData(75, "standard", 0)]
    [InlineData(100, "express", 12.99)]
    [InlineData(150, "express", 0)]
    [InlineData(100, "priority", 24.99)]
    [InlineData(200, "priority", 0)]
    public void ShippingFor_Tiers(decimal subtotal, string method, decimal expected) =>
        PricingRules.ShippingFor(subtotal, method).Should().Be(expected);

    [Fact]
    public void TaxFor_Is8Percent() => PricingRules.TaxFor(100m).Should().Be(8m);

    [Theory]
    [InlineData(100, 125, 20)]
    [InlineData(100, null, null)]
    [InlineData(100, 100, null)]
    public void DiscountPct_Rules(int price, int? compare, int? expected) =>
        PricingRules.DiscountPct(price, compare == null ? null : (decimal)compare).Should().Be(expected);

    [Theory]
    [InlineData("Placed", "Processing", true)]
    [InlineData("Placed", "Cancelled", true)]
    [InlineData("Placed", "Delivered", true)]
    [InlineData("Processing", "Cancelled", true)]
    [InlineData("Shipped", "Refunded", true)]
    [InlineData("Delivered", "Placed", false)]
    [InlineData("Placed", "Bogus", false)]
    public void Transitions_Validated(string from, string to, bool expected) =>
        PricingRules.IsValidTransition(from, to).Should().Be(expected);
}
