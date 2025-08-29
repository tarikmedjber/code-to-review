namespace MedjCap.Data.Trading.Models;

/// <summary>
/// Represents the minimum and maximum price values in a dataset.
/// Used for statistical summary and range analysis.
/// </summary>
public record PriceRange
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
}