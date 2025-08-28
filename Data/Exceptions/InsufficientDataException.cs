namespace MedjCap.Data.Exceptions;

/// <summary>
/// Exception thrown when there is insufficient data to perform statistical or ML operations.
/// Provides specific guidance on minimum data requirements and current data availability.
/// </summary>
public class InsufficientDataException : MedjCapException
{
    /// <summary>
    /// Number of samples required for the operation
    /// </summary>
    public int RequiredSamples { get; }

    /// <summary>
    /// Number of samples actually available
    /// </summary>
    public int ActualSamples { get; }

    /// <summary>
    /// Type of operation that failed due to insufficient data
    /// </summary>
    public string OperationType { get; }

    /// <summary>
    /// Recommended action to resolve the issue
    /// </summary>
    public string RecommendedAction { get; }

    public InsufficientDataException(
        string operationType,
        int requiredSamples,
        int actualSamples,
        string? additionalGuidance = null,
        Exception? innerException = null)
        : base(
            errorCode: "INSUFFICIENT_DATA",
            message: $"Insufficient data for {operationType}: required {requiredSamples}, got {actualSamples}",
            userMessage: CreateUserMessage(operationType, requiredSamples, actualSamples, additionalGuidance),
            context: CreateContext(operationType, requiredSamples, actualSamples, additionalGuidance),
            innerException: innerException)
    {
        RequiredSamples = requiredSamples;
        ActualSamples = actualSamples;
        OperationType = operationType;
        RecommendedAction = CreateRecommendedAction(operationType, requiredSamples, actualSamples);
    }

    private static string CreateUserMessage(string operationType, int required, int actual, string? guidance)
    {
        var baseMessage = $"Cannot perform {operationType} - need at least {required} data points, but only {actual} available.";
        
        if (!string.IsNullOrWhiteSpace(guidance))
        {
            baseMessage += $" {guidance}";
        }

        return baseMessage + " Please collect more data before retrying this analysis.";
    }

    private static Dictionary<string, object> CreateContext(string operationType, int required, int actual, string? guidance)
    {
        return new Dictionary<string, object>
        {
            ["OperationType"] = operationType,
            ["RequiredSamples"] = required,
            ["ActualSamples"] = actual,
            ["DataShortfall"] = required - actual,
            ["AdditionalGuidance"] = guidance ?? "None"
        };
    }

    private static string CreateRecommendedAction(string operationType, int required, int actual)
    {
        var shortfall = required - actual;
        return operationType.ToLowerInvariant() switch
        {
            var x when x.Contains("correlation") => 
                $"Collect at least {shortfall} more price/measurement data points to enable correlation analysis.",
            var x when x.Contains("ml") || x.Contains("optimization") => 
                $"Gather {shortfall} additional training samples for reliable ML model training.",
            var x when x.Contains("backtest") => 
                $"Extend the data period to include {shortfall} more historical data points for backtesting.",
            var x when x.Contains("validation") => 
                $"Increase sample size by {shortfall} to meet cross-validation requirements.",
            _ => $"Collect {shortfall} more data samples to proceed with {operationType}."
        };
    }
}