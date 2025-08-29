using MedjCap.Data.Trading.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.MachineLearning.Models;
using MedjCap.Data.Statistics.Models;
using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.Statistics.Correlation.Models;

namespace MedjCap.Data.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when correlation calculations fail due to invalid data or mathematical issues.
/// Provides detailed information about the correlation type and problematic values.
/// </summary>
public class CorrelationCalculationException : MedjCapException
{
    /// <summary>
    /// Type of correlation that failed (Pearson, Spearman, etc.)
    /// </summary>
    public CorrelationType CorrelationType { get; }

    /// <summary>
    /// Values that caused the calculation to fail
    /// </summary>
    public double[] InvalidValues { get; }

    /// <summary>
    /// Specific reason for the calculation failure
    /// </summary>
    public CorrelationFailureReason FailureReason { get; }

    /// <summary>
    /// Suggested data cleaning or preprocessing steps
    /// </summary>
    public string[] SuggestedFixes { get; }

    public CorrelationCalculationException(
        CorrelationType correlationType,
        CorrelationFailureReason failureReason,
        double[] invalidValues,
        string? additionalContext = null,
        Exception? innerException = null)
        : base(
            errorCode: "CORRELATION_CALCULATION_FAILED",
            message: $"{correlationType} correlation calculation failed: {failureReason}",
            userMessage: CreateUserMessage(correlationType, failureReason, invalidValues),
            context: CreateContext(correlationType, failureReason, invalidValues, additionalContext),
            innerException: innerException)
    {
        CorrelationType = correlationType;
        InvalidValues = invalidValues ?? Array.Empty<double>();
        FailureReason = failureReason;
        SuggestedFixes = CreateSuggestedFixes(failureReason, InvalidValues);
    }

    private static string CreateUserMessage(CorrelationType type, CorrelationFailureReason reason, double[] values)
    {
        var baseMessage = $"Failed to calculate {type} correlation due to {reason.ToString().ToLowerInvariant().Replace('_', ' ')}.";
        
        return reason switch
        {
            CorrelationFailureReason.ContainsNaN => 
                $"{baseMessage} Found {values.Count(double.IsNaN)} NaN values in the dataset.",
            CorrelationFailureReason.ContainsInfinity => 
                $"{baseMessage} Found {values.Count(double.IsInfinity)} infinite values in the dataset.",
            CorrelationFailureReason.ZeroVariance => 
                $"{baseMessage} One or both data series have zero variance (all values are identical).",
            CorrelationFailureReason.DivisionByZero => 
                $"{baseMessage} Mathematical division by zero occurred during calculation.",
            CorrelationFailureReason.InvalidDataRange => 
                $"{baseMessage} Data values are outside the valid range for this correlation type.",
            _ => baseMessage
        };
    }

    private static Dictionary<string, object> CreateContext(
        CorrelationType type, 
        CorrelationFailureReason reason, 
        double[] values,
        string? additionalContext)
    {
        var context = new Dictionary<string, object>
        {
            ["CorrelationType"] = type.ToString(),
            ["FailureReason"] = reason.ToString(),
            ["InvalidValueCount"] = values?.Length ?? 0,
            ["AdditionalContext"] = additionalContext ?? "None"
        };

        if (values?.Any() == true)
        {
            context["FirstFewInvalidValues"] = values.Take(5).ToArray();
            context["HasNaN"] = values.Any(double.IsNaN);
            context["HasInfinity"] = values.Any(double.IsInfinity);
            context["MinValue"] = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).DefaultIfEmpty(0).Min();
            context["MaxValue"] = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).DefaultIfEmpty(0).Max();
        }

        return context;
    }

    private static string[] CreateSuggestedFixes(CorrelationFailureReason reason, double[] values)
    {
        return reason switch
        {
            CorrelationFailureReason.ContainsNaN => new[]
            {
                "Remove or interpolate NaN values in your dataset",
                "Check data collection process for missing value handling",
                "Use correlation methods that handle missing data gracefully"
            },
            CorrelationFailureReason.ContainsInfinity => new[]
            {
                "Cap extreme values using winsorization or trimming",
                "Check for division by zero in data preprocessing",
                "Apply log transformation to reduce extreme values"
            },
            CorrelationFailureReason.ZeroVariance => new[]
            {
                "Verify that both measurement and price data vary over time",
                "Check if data collection is working correctly",
                "Ensure sufficient time range for data collection"
            },
            CorrelationFailureReason.DivisionByZero => new[]
            {
                "Add small epsilon value to avoid division by zero",
                "Check variance calculations in preprocessing",
                "Validate that standard deviation is non-zero"
            },
            CorrelationFailureReason.InvalidDataRange => new[]
            {
                "Normalize data values to appropriate range",
                "Check for outliers that may affect correlation calculation",
                "Apply appropriate data transformations before correlation analysis"
            },
            _ => new[] { "Review data quality and preprocessing steps" }
        };
    }
}

/// <summary>
/// Specific reasons why correlation calculation can fail
/// </summary>
public enum CorrelationFailureReason
{
    ContainsNaN,
    ContainsInfinity,
    ZeroVariance,
    DivisionByZero,
    InvalidDataRange,
    MathematicalError,
    UnknownError
}