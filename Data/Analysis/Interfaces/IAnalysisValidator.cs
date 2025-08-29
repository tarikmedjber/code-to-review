using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.TimeSeries.Models;

namespace MedjCap.Data.Analysis.Interfaces;

/// <summary>
/// Validates analysis requests to ensure they contain proper parameters and meet business rules.
/// Extracted from AnalysisEngine to follow Single Responsibility Principle.
/// </summary>
public interface IAnalysisValidator
{
    /// <summary>
    /// Validates a standard analysis request.
    /// </summary>
    /// <param name="request">The analysis request to validate</param>
    /// <returns>Validation result with any errors or warnings</returns>
    ValidationResult ValidateRequest(AnalysisRequest request);
    
    /// <summary>
    /// Validates a multi-measurement analysis request.
    /// </summary>
    /// <param name="request">The multi-measurement analysis request to validate</param>
    /// <returns>Validation result with any errors or warnings</returns>
    ValidationResult ValidateRequest(MultiMeasurementAnalysisRequest request);
    
    /// <summary>
    /// Validates a contextual analysis request.
    /// </summary>
    /// <param name="request">The contextual analysis request to validate</param>
    /// <returns>Validation result with any errors or warnings</returns>
    ValidationResult ValidateRequest(ContextualAnalysisRequest request);

    /// <summary>
    /// Throws appropriate validation exception if validation fails.
    /// </summary>
    /// <param name="validationResult">Validation result to check</param>
    /// <param name="requestType">Type of request being validated</param>
    void ThrowIfInvalid(ValidationResult validationResult, string requestType);
}

/// <summary>
/// Result of request validation containing errors, warnings, and validated data.
/// </summary>
public record RequestValidationResult
{
    /// <summary>
    /// Whether the request passed validation.
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Validation errors that prevent processing.
    /// </summary>
    public List<string> Errors { get; init; } = new();
    
    /// <summary>
    /// Validation warnings that don't prevent processing.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// Additional validation context and metadata.
    /// </summary>
    public Dictionary<string, object> ValidationContext { get; init; } = new();
}