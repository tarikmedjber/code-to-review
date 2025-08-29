namespace MedjCap.Data.Infrastructure.Models;

/// <summary>
/// Represents a time range for analysis periods.
/// Used for defining in-sample, out-of-sample, and walk-forward testing windows.
/// </summary>
public record DateRange
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }

    /// <summary>
    /// Gets the duration of this date range.
    /// </summary>
    public TimeSpan Duration => End - Start;

    /// <summary>
    /// Checks if the specified date falls within this range (inclusive).
    /// </summary>
    public bool Contains(DateTime date) => date >= Start && date <= End;

    /// <summary>
    /// Checks if this range overlaps with another range.
    /// </summary>
    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;
}