using Ecommerce.Core.Dtos;
using FluentValidation;

namespace Ecommerce.Api.Validation;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100)
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class AddCartValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 99);
    }
}

public class UpdateCartValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartValidator() => RuleFor(x => x.Quantity).InclusiveBetween(0, 99);
}

public class CheckoutValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutValidator()
    {
        RuleFor(x => x.ShippingMethod).NotEmpty().Must(m => new[] { "standard", "express", "priority" }.Contains((m ?? "").ToLower())).WithMessage("Invalid shipping method.");
        RuleFor(x => x.PaymentMethod).NotEmpty().Must(m => new[] { "cod", "card_demo", "upi_demo", "netbanking_demo" }.Contains((m ?? "").ToLower())).WithMessage("Invalid payment method.");
        RuleFor(x => x.ShippingAddress).NotNull().SetValidator(new AddressInputValidator());
        RuleFor(x => x.BillingAddress).NotNull().SetValidator(new AddressInputValidator());
    }
}

public class AddressInputValidator : AbstractValidator<AddressInput>
{
    public AddressInputValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(250);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(80);
    }
}

public class ProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public ProfileValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class AddressValidator : AbstractValidator<CreateAddressRequest>
{
    public AddressValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(250);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(80);
    }
}

public class AdminProductValidator : AbstractValidator<AdminProductRequest>
{
    public AdminProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100000);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ImageUrl).MaximumLength(1000)
            .Must(u => string.IsNullOrWhiteSpace(u) || u.StartsWith('/') || u.StartsWith("http://") || u.StartsWith("https://"))
            .WithMessage("Image must be a site-relative path (/) or an http(s) URL.");
    }
}

public class AdminCategoryValidator : AbstractValidator<AdminCategoryRequest>
{
    public AdminCategoryValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
}
