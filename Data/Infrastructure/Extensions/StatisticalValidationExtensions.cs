using System.Collections;

namespace MedjCap.Data.Infrastructure.Extensions;

/// <summary>
/// Extension methods for statistical validation and complex condition checking.
/// Improves code readability by extracting inline boolean logic into meaningful methods.
/// </summary>
public static class StatisticalValidationExtensions
{
    /// <summary>
    /// Determines if a decimal value falls within a specified range (inclusive).
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="min">Minimum value (inclusive)</param>
    /// <param name="max">Maximum value (inclusive)</param>
    /// <returns>True if value is within range, false otherwise</returns>
    public static bool IsWithinRange(this decimal value, decimal min, decimal max)
        => value >= min && value <= max;

    /// <summary>
    /// Determines if a double value falls within a specified range (inclusive).
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="min">Minimum value (inclusive)</param>
    /// <param name="max">Maximum value (inclusive)</param>
    /// <returns>True if value is within range, false otherwise</returns>
    public static bool IsWithinRange(this double value, double min, double max)
        => value >= min && value <= max;

    /// <summary>
    /// Determines if a p-value indicates statistical significance.
    /// </summary>
    /// <param name="pValue">The p-value to test</param>
    /// <param name="alpha">Significance level (default: 0.05)</param>
    /// <returns>True if result is statistically significant, false otherwise</returns>
    public static bool IsStatisticallySignificant(this double pValue, double alpha = 0.05)
        => pValue < alpha;

    /// <summary>
    /// Determines if a collection has sufficient samples for statistical analysis.
    /// </summary>
    /// <param name="collection">The collection to check</param>
    /// <param name="minimumRequired">Minimum required sample size</param>
    /// <returns>True if collection has sufficient samples, false otherwise</returns>
    public static bool HasSufficientSamples(this ICollection collection, int minimumRequired)
        => collection?.Count >= minimumRequired;

    /// <summary>
    /// Determines if a correlation coefficient represents a strong correlation.
    /// </summary>
    /// <param name="coefficient">The correlation coefficient</param>
    /// <param name="threshold">Threshold for strong correlation (default: 0.3)</param>
    /// <returns>True if correlation is considered strong, false otherwise</returns>
    public static bool IsStrongCorrelation(this double coefficient, double threshold = 0.3)
        => Math.Abs(coefficient) > threshold;

    /// <summary>
    /// Determines if a measurement value is in the bullish trading range.
    /// </summary>
    /// <param name="measurementValue">The measurement value to check</param>
    /// <param name="lowerBound">Lower bound of bullish range (default: 60)</param>
    /// <param name="upperBound">Upper bound of bullish range (default: 70)</param>
    /// <returns>True if value is in bullish range, false otherwise</returns>
    public static bool IsInBullishRange(this decimal measurementValue, decimal lowerBound = 60, decimal upperBound = 70)
        => measurementValue.IsWithinRange(lowerBound, upperBound);

    /// <summary>
    /// Determines if a measurement value is in the bearish trading range.
    /// </summary>
    /// <param name="measurementValue">The measurement value to check</param>
    /// <param name="lowerBound">Lower bound of bearish range (default: 30)</param>
    /// <param name="upperBound">Upper bound of bearish range (default: 40)</param>
    /// <returns>True if value is in bearish range, false otherwise</returns>
    public static bool IsInBearishRange(this decimal measurementValue, decimal lowerBound = 30, decimal upperBound = 40)
        => measurementValue.IsWithinRange(lowerBound, upperBound);

    /// <summary>
    /// Determines if a correlation coefficient is above the acceptable threshold.
    /// </summary>
    /// <param name="coefficient">The correlation coefficient</param>
    /// <param name="threshold">Minimum acceptable correlation (default: 0.5)</param>
    /// <returns>True if correlation meets threshold, false otherwise</returns>
    public static bool MeetsCorrelationThreshold(this double coefficient, double threshold = 0.5)
        => coefficient >= threshold;

    /// <summary>
    /// Determines if the sample size is adequate for reliable analysis.
    /// </summary>
    /// <param name="sampleSize">The sample size to validate</param>
    /// <param name="minimumRequired">Minimum required sample size (default: 100)</param>
    /// <returns>True if sample size is adequate, false otherwise</returns>
    public static bool IsAdequateSampleSize(this int sampleSize, int minimumRequired = 100)
        => sampleSize >= minimumRequired;

    /// <summary>
    /// Determines if performance degradation is within acceptable limits.
    /// </summary>
    /// <param name="degradation">Performance degradation value</param>
    /// <param name="maxAcceptable">Maximum acceptable degradation (default: 0.3)</param>
    /// <returns>True if degradation is acceptable, false otherwise</returns>
    public static bool IsAcceptableDegradation(this double degradation, double maxAcceptable = 0.3)
        => degradation <= maxAcceptable;

    /// <summary>
    /// Determines if a price value is valid (positive).
    /// </summary>
    /// <param name="price">The price to validate</param>
    /// <returns>True if price is positive, false otherwise</returns>
    public static bool IsValidPrice(this decimal price)
        => price > 0;

    /// <summary>
    /// Determines if an ATR (Average True Range) value is valid (positive).
    /// </summary>
    /// <param name="atr">The ATR value to validate</param>
    /// <returns>True if ATR is positive, false otherwise</returns>
    public static bool IsValidAtr(this decimal atr)
        => atr > 0;
}