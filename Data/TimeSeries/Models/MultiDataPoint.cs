namespace MedjCap.Data.TimeSeries.Models;

/// <summary>
/// Represents multiple measurement observations at a single point in time.
/// Used for storing multiple indicator values simultaneously with market context.
/// </summary>
public record MultiDataPoint
{
    public string Id { get; set; } = string.Empty; // Unique identifier for storage
    public DateTime Timestamp { get; init; }
    public Dictionary<string, decimal> Measurements { get; init; } = new();  // Multiple measurements at once
    public decimal Price { get; init; }
    public decimal ATR { get; init; }
    public Dictionary<string, decimal> ContextualData { get; init; } = new();
}