using MedjCap.Data.Configuration;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using MedjCap.Data.Exceptions;
using Microsoft.Extensions.Options;

namespace MedjCap.Data.Services;

/// <summary>
/// Validates analysis requests to ensure they contain proper parameters and meet business rules.
/// Extracted from AnalysisEngine to follow Single Responsibility Principle.
/// </summary>
public class AnalysisValidator : IAnalysisValidator
{
    private readonly ValidationConfig _config;
    private readonly StatisticalConfig _statisticalConfig;

    public AnalysisValidator(IOptions<ValidationConfig> config, IOptions<StatisticalConfig> statisticalConfig)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _statisticalConfig = statisticalConfig?.Value ?? throw new ArgumentNullException(nameof(statisticalConfig));
    }

    public ValidationResult ValidateRequest(AnalysisRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var errors = new List<string>();
        var warnings = new List<string>();
        var context = new Dictionary<string, object>();

        // Validate MeasurementId
        if (string.IsNullOrWhiteSpace(request.MeasurementId))
        {
            errors.Add("MeasurementId cannot be null or empty");
        }
        else
        {
            context["MeasurementId"] = request.MeasurementId;
        }

        // Validate TimeHorizons
        if (request.TimeHorizons == null || request.TimeHorizons.Length == 0)
        {
            errors.Add("TimeHorizons cannot be null or empty");
        }
        else
        {
            context["TimeHorizonCount"] = request.TimeHorizons.Length;
            
            // Check for reasonable time horizons
            var invalidHorizons = request.TimeHorizons.Where(h => h.TotalMinutes < 1 || h.TotalDays > 365).ToList();
            if (invalidHorizons.Any())
            {
                warnings.Add($"Some time horizons may be unrealistic: {string.Join(", ", invalidHorizons.Select(h => h.ToString()))}");
            }

            // Check for duplicate time horizons
            var duplicates = request.TimeHorizons.GroupBy(h => h).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Any())
            {
                warnings.Add($"Duplicate time horizons detected: {string.Join(", ", duplicates.Select(h => h.ToString()))}");
            }
        }

        // Validate ATRTargets
        if (request.ATRTargets == null || request.ATRTargets.Length == 0)
        {
            errors.Add("ATRTargets cannot be null or empty");
        }
        else
        {
            context["ATRTargetCount"] = request.ATRTargets.Length;
            
            // Check for reasonable ATR targets
            var negativeTargets = request.ATRTargets.Where(atr => atr < 0).ToList();
            if (negativeTargets.Any())
            {
                errors.Add($"ATR targets cannot be negative: {string.Join(", ", negativeTargets)}");
            }

            var excessiveTargets = request.ATRTargets.Where(atr => atr > 10).ToList();
            if (excessiveTargets.Any())
            {
                warnings.Add($"Very large ATR targets detected (>10): {string.Join(", ", excessiveTargets)}");
            }
        }

        // Validate Config
        if (request.Config != null)
        {
            ValidateAnalysisConfig(request.Config, errors, warnings, context);
        }
        else
        {
            warnings.Add("No analysis configuration provided, using defaults");
        }

        return new ValidationResult
        {
            InSamplePerformance = 0, // Not applicable for request validation
            OutOfSamplePerformance = 0, // Not applicable for request validation
            PerformanceDegradation = 0, // Not applicable for request validation
            IsOverfitted = false, // Not applicable for request validation
            ValidationMetrics = new Dictionary<string, double>
            {
                ["ErrorCount"] = errors.Count,
                ["WarningCount"] = warnings.Count,
                ["IsValid"] = errors.Count == 0 ? 1.0 : 0.0
            }
        };
    }

    public ValidationResult ValidateRequest(MultiMeasurementAnalysisRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var errors = new List<string>();
        var warnings = new List<string>();
        var context = new Dictionary<string, object>();

        // Validate MeasurementIds
        if (request.MeasurementIds == null || request.MeasurementIds.Length == 0)
        {
            errors.Add("MeasurementIds cannot be null or empty");
        }
        else
        {
            context["MeasurementCount"] = request.MeasurementIds.Length;

            // Check for duplicates
            var duplicates = request.MeasurementIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Any())
            {
                errors.Add($"Duplicate measurement IDs detected: {string.Join(", ", duplicates)}");
            }

            // Check for null/empty IDs
            var emptyIds = request.MeasurementIds.Where(id => string.IsNullOrWhiteSpace(id)).ToList();
            if (emptyIds.Any())
            {
                errors.Add($"Found {emptyIds.Count} null or empty measurement IDs");
            }

            // Warn about excessive measurements
            if (request.MeasurementIds.Length > 20)
            {
                warnings.Add($"Large number of measurements ({request.MeasurementIds.Length}) may impact performance");
            }
        }

        // Validate TimeHorizons (similar to single measurement)
        if (request.TimeHorizons == null || request.TimeHorizons.Length == 0)
        {
            errors.Add("TimeHorizons cannot be null or empty");
        }

        // Validate ATRTargets (similar to single measurement)
        if (request.ATRTargets == null || request.ATRTargets.Length == 0)
        {
            errors.Add("ATRTargets cannot be null or empty");
        }

        // Validate OptimizationTarget
        if (!Enum.IsDefined(typeof(OptimizationTarget), request.OptimizationTarget))
        {
            errors.Add($"Invalid optimization target: {request.OptimizationTarget}");
        }

        // Validate InitialWeights if provided
        if (request.InitialWeights?.Count > 0)
        {
            context["InitialWeightsProvided"] = true;
            
            var invalidWeights = request.InitialWeights.Where(w => w.Value < 0 || w.Value > 1).ToList();
            if (invalidWeights.Any())
            {
                errors.Add($"Initial weights must be between 0 and 1: {string.Join(", ", invalidWeights.Select(w => $"{w.Key}={w.Value}"))}");
            }

            var totalWeight = request.InitialWeights.Values.Sum();
            if (Math.Abs(totalWeight - 1.0) > 0.01)
            {
                warnings.Add($"Initial weights sum to {totalWeight:F3}, expected 1.0");
            }
        }

        return new ValidationResult
        {
            ValidationMetrics = new Dictionary<string, double>
            {
                ["ErrorCount"] = errors.Count,
                ["WarningCount"] = warnings.Count,
                ["IsValid"] = errors.Count == 0 ? 1.0 : 0.0
            }
        };
    }

    public ValidationResult ValidateRequest(ContextualAnalysisRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var errors = new List<string>();
        var warnings = new List<string>();
        var context = new Dictionary<string, object>();

        // Validate ContextVariable
        if (string.IsNullOrWhiteSpace(request.ContextVariable))
        {
            errors.Add("ContextVariable cannot be null or empty");
        }
        else
        {
            context["ContextVariable"] = request.ContextVariable;
        }

        // Validate ContextThresholds
        if (request.ContextThresholds == null)
        {
            errors.Add("ContextThresholds cannot be null");
        }
        else
        {
            context["ThresholdCount"] = request.ContextThresholds.Length;

            if (request.ContextThresholds.Length == 0)
            {
                warnings.Add("No context thresholds provided, analysis may not be meaningful");
            }
            else if (request.ContextThresholds.Length > 10)
            {
                warnings.Add($"Large number of thresholds ({request.ContextThresholds.Length}) may create too many small groups");
            }

            // Check for proper ordering
            var sortedThresholds = request.ContextThresholds.OrderBy(t => t).ToArray();
            if (!request.ContextThresholds.SequenceEqual(sortedThresholds))
            {
                warnings.Add("Context thresholds should be provided in ascending order for best results");
            }

            // Check for duplicates
            var duplicates = request.ContextThresholds.GroupBy(t => t).Where(g => g.Count() > 1).ToList();
            if (duplicates.Any())
            {
                errors.Add($"Duplicate context thresholds detected: {string.Join(", ", duplicates.Select(g => g.Key))}");
            }
        }

        // Validate TimeHorizon
        if (request.TimeHorizon.TotalMinutes < 1)
        {
            errors.Add("TimeHorizon must be at least 1 minute");
        }
        else if (request.TimeHorizon.TotalDays > 365)
        {
            warnings.Add($"Very long time horizon ({request.TimeHorizon.TotalDays:F1} days) may not be meaningful");
        }

        return new ValidationResult
        {
            ValidationMetrics = new Dictionary<string, double>
            {
                ["ErrorCount"] = errors.Count,
                ["WarningCount"] = warnings.Count,
                ["IsValid"] = errors.Count == 0 ? 1.0 : 0.0
            }
        };
    }

    /// <summary>
    /// Validates analysis configuration settings.
    /// </summary>
    private void ValidateAnalysisConfig(AnalysisConfig config, List<string> errors, List<string> warnings, Dictionary<string, object> context)
    {
        // Validate date ranges
        if (config.InSample.Start >= config.InSample.End)
        {
            errors.Add("InSample start date must be before end date");
        }

        var inSampleDays = (config.InSample.End - config.InSample.Start).TotalDays;
        context["InSampleDays"] = inSampleDays;

        if (inSampleDays < 30)
        {
            warnings.Add($"Short in-sample period ({inSampleDays:F1} days) may not provide reliable results");
        }
        else if (inSampleDays > 2 * 365)
        {
            warnings.Add($"Very long in-sample period ({inSampleDays:F1} days) may include regime changes");
        }

        // Validate walk-forward windows
        if (config.WalkForwardWindows <= 0)
        {
            errors.Add("WalkForwardWindows must be positive");
        }
        else if (config.WalkForwardWindows > _config.MaxWalkForwardWindows)
        {
            errors.Add($"WalkForwardWindows cannot exceed {_config.MaxWalkForwardWindows}");
        }
        else if (config.WalkForwardWindows < 3)
        {
            warnings.Add($"Few walk-forward windows ({config.WalkForwardWindows}) may not provide robust validation");
        }

        context["WalkForwardWindows"] = config.WalkForwardWindows;
    }

    /// <summary>
    /// Throws appropriate validation exception if validation fails.
    /// </summary>
    public void ThrowIfInvalid(ValidationResult validationResult, string requestType)
    {
        if (validationResult.ValidationMetrics.GetValueOrDefault("IsValid", 0) == 0)
        {
            var errorCount = (int)validationResult.ValidationMetrics.GetValueOrDefault("ErrorCount", 0);
            throw new DataValidationException(
                "Request",
                requestType,
                DataValidationType.BusinessRule,
                $"Request validation failed with {errorCount} error(s). See ValidationMetrics for details.");
        }
    }
}