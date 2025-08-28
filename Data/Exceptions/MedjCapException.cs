namespace MedjCap.Data.Exceptions;

/// <summary>
/// Base exception class for all MedjCap domain-specific exceptions.
/// Provides structured error information with context for troubleshooting and recovery.
/// </summary>
public abstract class MedjCapException : Exception
{
    /// <summary>
    /// Unique error code identifying the specific type of error
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// User-friendly error message with actionable guidance
    /// </summary>
    public string UserMessage { get; }

    /// <summary>
    /// Additional context information for debugging and analysis
    /// </summary>
    public Dictionary<string, object> Context { get; }

    /// <summary>
    /// Timestamp when the exception occurred
    /// </summary>
    public DateTime OccurredAt { get; }

    protected MedjCapException(
        string errorCode,
        string message,
        string userMessage,
        Dictionary<string, object>? context = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        UserMessage = userMessage ?? throw new ArgumentNullException(nameof(userMessage));
        Context = context ?? new Dictionary<string, object>();
        OccurredAt = DateTime.UtcNow;

        // Add standard context information
        Context["ErrorCode"] = ErrorCode;
        Context["OccurredAt"] = OccurredAt;
        Context["ExceptionType"] = GetType().Name;
    }

    /// <summary>
    /// Creates a structured error report for logging and monitoring
    /// </summary>
    public virtual Dictionary<string, object> CreateErrorReport()
    {
        var report = new Dictionary<string, object>
        {
            ["ErrorCode"] = ErrorCode,
            ["Message"] = Message,
            ["UserMessage"] = UserMessage,
            ["OccurredAt"] = OccurredAt,
            ["ExceptionType"] = GetType().Name
        };

        // Add context information
        foreach (var kvp in Context)
        {
            report[$"Context.{kvp.Key}"] = kvp.Value;
        }

        // Add inner exception details if present
        if (InnerException != null)
        {
            report["InnerException"] = new
            {
                Type = InnerException.GetType().Name,
                Message = InnerException.Message,
                StackTrace = InnerException.StackTrace
            };
        }

        return report;
    }

    public override string ToString()
    {
        var contextStr = Context.Any() 
            ? $" Context: {string.Join(", ", Context.Select(kvp => $"{kvp.Key}={kvp.Value}"))}"
            : "";
        
        return $"[{ErrorCode}] {UserMessage}: {Message}{contextStr}";
    }
}