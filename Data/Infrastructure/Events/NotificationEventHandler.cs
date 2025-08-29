using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedjCap.Data.Analysis.Events;
using MedjCap.Data.MachineLearning.Events;
using MedjCap.Data.Trading.Events;

namespace MedjCap.Data.Infrastructure.Events;

public class NotificationEventHandler
{
    private readonly ILogger<NotificationEventHandler> _logger;

    public NotificationEventHandler(ILogger<NotificationEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void HandleAnalysisCompleted(AnalysisCompletedEvent evt)
    {
        if (evt.IsSignificant)
        {
            _logger.LogInformation("Significant correlation detected: {Coefficient:F4} for measurement {MeasurementId} (Sample: {SampleCount}, Duration: {Duration})", 
                evt.CorrelationCoefficient, evt.MeasurementId, evt.SampleCount, evt.AnalysisDuration);

            // Add to metadata for further processing
            evt.Metadata["NotificationSent"] = DateTime.UtcNow;
            evt.Metadata["NotificationType"] = "SignificantCorrelation";
        }
        else
        {
            _logger.LogDebug("Analysis completed for measurement {MeasurementId}: correlation {Coefficient:F4} not significant", 
                evt.MeasurementId, evt.CorrelationCoefficient);
        }
    }

    public async Task HandleThresholdBreached(ThresholdBreachedEvent evt)
    {
        var direction = evt.Direction == ThresholdDirection.Above ? "above" : "below";
        
        _logger.LogWarning("Threshold breach detected: {ThresholdName} {Direction} threshold {ThresholdValue:F4}. Actual value: {ActualValue:F4} for measurement {MeasurementId}",
            evt.ThresholdName, direction, evt.ThresholdValue, evt.ActualValue, evt.MeasurementId);

        // Simulate alerting service call
        await Task.Delay(10); // Simulate network call
        
        evt.Metadata["AlertSent"] = DateTime.UtcNow;
        evt.Metadata["Severity"] = DetermineSeverity(evt);
        
        _logger.LogInformation("Alert sent for threshold breach {EventId}", evt.EventId);
    }

    public void HandleDataQualityIssue(DataQualityIssueDetectedEvent evt)
    {
        _logger.LogWarning("Data quality issue detected: {Issue} affecting {AffectedDataPoints} data points for measurement {MeasurementId}. Recommended action: {RecommendedAction}",
            evt.Issue, evt.AffectedDataPoints, evt.MeasurementId, evt.RecommendedAction);

        evt.Metadata["IssueLogged"] = DateTime.UtcNow;
        evt.Metadata["RequiresIntervention"] = evt.Issue == DataQualityIssue.InsufficientData || evt.Issue == DataQualityIssue.CorrelationDegradation;
    }

    public async Task HandleOptimizationCompleted(OptimizationCompletedEvent evt)
    {
        _logger.LogInformation("Optimization completed: {OptimizationType} using {MethodUsed}. Found {BoundariesFound} boundaries with confidence {ConfidenceScore:F4}. Duration: {Duration}",
            evt.OptimizationType, evt.MethodUsed, evt.BoundariesFound, evt.ConfidenceScore, evt.Duration);

        // Simulate performance tracking
        await Task.Delay(5);
        
        evt.Metadata["PerformanceTracked"] = DateTime.UtcNow;
        evt.Metadata["OptimizationQuality"] = evt.ConfidenceScore > 0.7 ? "High" : evt.ConfidenceScore > 0.4 ? "Medium" : "Low";
    }

    public void HandleCorrelationDegradation(CorrelationDegradationEvent evt)
    {
        _logger.LogWarning("Correlation degradation detected for measurement {MeasurementId}: {PreviousCorrelation:F4} → {CurrentCorrelation:F4} ({DegradationPercentage:F1}% degradation)",
            evt.MeasurementId, evt.PreviousCorrelation, evt.CurrentCorrelation, evt.DegradationPercentage);

        evt.Metadata["DegradationSeverity"] = evt.DegradationPercentage > 50 ? "Critical" : evt.DegradationPercentage > 25 ? "Warning" : "Info";
        evt.Metadata["RequiresReoptimization"] = evt.DegradationPercentage > 30;
    }

    public async Task HandleOutlierDetected(OutlierDetectedEvent evt)
    {
        _logger.LogInformation("Outliers detected for measurement {MeasurementId}: {OutlierCount} outliers using {DetectionMethod}. Action taken: {ActionTaken}",
            evt.MeasurementId, evt.OutlierCount, evt.DetectionMethod, evt.ActionTaken);

        // Simulate outlier analysis
        await Task.Delay(2);
        
        evt.Metadata["OutlierAnalysisCompleted"] = DateTime.UtcNow;
        evt.Metadata["DataQualityImpact"] = evt.OutlierCount > 10 ? "High" : evt.OutlierCount > 3 ? "Medium" : "Low";
    }

    public void HandleBacktestCompleted(BacktestCompletedEvent evt)
    {
        _logger.LogInformation("Backtest completed for {BacktestId}: Performance {OverallPerformance:F4}, Win Rate {WinRate:P1}, Total Trades {TotalTrades} over {BacktestPeriod}",
            evt.BacktestId, evt.OverallPerformance, evt.WinRate, evt.TotalTrades, evt.BacktestPeriod);

        evt.Metadata["BacktestQuality"] = evt.WinRate > 0.6 ? "Good" : evt.WinRate > 0.4 ? "Fair" : "Poor";
        evt.Metadata["TradeFrequency"] = evt.TotalTrades / evt.BacktestPeriod.TotalDays;
    }

    private static string DetermineSeverity(ThresholdBreachedEvent evt)
    {
        var deviation = Math.Abs(evt.ActualValue - evt.ThresholdValue) / Math.Abs(evt.ThresholdValue);
        
        return deviation switch
        {
            > 0.5 => "Critical",
            > 0.2 => "High",
            > 0.1 => "Medium",
            _ => "Low"
        };
    }
}