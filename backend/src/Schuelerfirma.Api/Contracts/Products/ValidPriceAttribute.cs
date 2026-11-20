using System.ComponentModel.DataAnnotations;
using Schuelerfirma.Api.Application.Products;

namespace Schuelerfirma.Api.Contracts.Products;

/// <summary>
/// A price is <c>null</c> or between 0 and the column maximum with at most two decimal places
/// (<see cref="ProductRules.IsValidPrice"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ValidPriceAttribute()
    : ValidationAttribute("Der Preis muss mindestens 0 sein und darf höchstens zwei Nachkommastellen haben.")
{
    /// <inheritdoc />
    public override bool IsValid(object? value) => value is null || value is decimal price && ProductRules.IsValidPrice(price);
}
