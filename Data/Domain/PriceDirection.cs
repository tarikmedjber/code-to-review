namespace MedjCap.Data.Domain;

/// <summary>
/// Represents the direction of a price movement.
/// Used for categorizing price movements in correlation analysis.
/// </summary>
public enum PriceDirection
{
    Up,
    Down,
    Flat
}