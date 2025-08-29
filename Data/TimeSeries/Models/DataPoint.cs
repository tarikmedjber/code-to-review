namespace MedjCap.Data.TimeSeries.Models;

/// <summary>
/// Represents a single measurement observation at a point in time.
/// Used for storing individual indicator values with market context.
/// </summary>
public record DataPoint
{
    public string Id { get; set; } = string.Empty; // Unique identifier for storage
    public DateTime Timestamp { get; init; }
    public string MeasurementId { get; init; } = string.Empty;  // e.g., "PriceVa_Score", "CustomIndicator"
    public decimal MeasurementValue { get; init; }
    public decimal Price { get; init; }
    public decimal ATR { get; init; }
    public Dictionary<string, decimal> ContextualData { get; init; } = new();
}