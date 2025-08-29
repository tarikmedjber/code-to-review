namespace MedjCap.Data.Trading.Models;

/// <summary>
/// Represents a price movement observation with measurement context.
/// Used for correlation analysis between indicator values and subsequent price moves.
/// </summary>
public record PriceMovement
{
    public DateTime StartTimestamp { get; init; }
    public decimal MeasurementValue { get; init; }
    public decimal ATRMovement { get; init; }
    public PriceDirection Direction => ATRMovement > 0 ? PriceDirection.Up : 
                                       ATRMovement < 0 ? PriceDirection.Down : 
                                       PriceDirection.Flat;
    public Dictionary<string, decimal> ContextualData { get; init; } = new();
}