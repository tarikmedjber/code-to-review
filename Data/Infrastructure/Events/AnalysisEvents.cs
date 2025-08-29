using System;
using System.Collections.Generic;
using MedjCap.Data.Infrastructure.Events.Core;

namespace MedjCap.Data.Infrastructure.Events;

public enum ThresholdDirection
{
    Above,
    Below
}

public enum DataQualityIssue
{
    InsufficientData,
    OutlierDetected,
    HighVariance,
    MissingValues,
    CorrelationDegradation,
    InvalidRange
}

public class AnalysisCompletedEvent : DomainEvent
{
    public string MeasurementId { get; init; } = string.Empty;
    public double CorrelationCoefficient { get; init; }
    public bool IsSignificant { get; init; }
    public int SampleCount { get; init; }
    public TimeSpan AnalysisDuration { get; init; }

    public AnalysisCompletedEvent() { }

    public AnalysisCompletedEvent(string measurementId) : base(measurementId)
    {
        MeasurementId = measurementId;
    }
}

public class OptimizationCompletedEvent : DomainEvent
{
    public string OptimizationType { get; init; } = string.Empty;
    public int BoundariesFound { get; init; }
    public double ConfidenceScore { get; init; }
    public TimeSpan Duration { get; init; }
    public string MethodUsed { get; init; } = string.Empty;

    public OptimizationCompletedEvent() { }

    public OptimizationCompletedEvent(string aggregateId) : base(aggregateId) { }
}

public class DataQualityIssueDetectedEvent : DomainEvent
{
    public DataQualityIssue Issue { get; init; }
    public string MeasurementId { get; init; } = string.Empty;
    public int AffectedDataPoints { get; init; }
    public string RecommendedAction { get; init; } = string.Empty;
    public Dictionary<string, object> IssueDetails { get; init; } = new();

    public DataQualityIssueDetectedEvent() { }

    public DataQualityIssueDetectedEvent(string measurementId) : base(measurementId)
    {
        MeasurementId = measurementId;
    }
}

public class ThresholdBreachedEvent : DomainEvent
{
    public string ThresholdName { get; init; } = string.Empty;
    public double ThresholdValue { get; init; }
    public double ActualValue { get; init; }
    public ThresholdDirection Direction { get; init; }
    public string MeasurementId { get; init; } = string.Empty;

    public ThresholdBreachedEvent() { }

    public ThresholdBreachedEvent(string measurementId) : base(measurementId)
    {
        MeasurementId = measurementId;
    }
}

public class CorrelationDegradationEvent : DomainEvent
{
    public string MeasurementId { get; init; } = string.Empty;
    public double PreviousCorrelation { get; init; }
    public double CurrentCorrelation { get; init; }
    public double DegradationPercentage { get; init; }
    public DateTime DetectionTime { get; init; } = DateTime.UtcNow;

    public CorrelationDegradationEvent() { }

    public CorrelationDegradationEvent(string measurementId) : base(measurementId)
    {
        MeasurementId = measurementId;
    }
}

public class OutlierDetectedEvent : DomainEvent
{
    public string MeasurementId { get; init; } = string.Empty;
    public int OutlierCount { get; init; }
    public string DetectionMethod { get; init; } = string.Empty;
    public double[] OutlierValues { get; init; } = Array.Empty<double>();
    public string ActionTaken { get; init; } = string.Empty;

    public OutlierDetectedEvent() { }

    public OutlierDetectedEvent(string measurementId) : base(measurementId)
    {
        MeasurementId = measurementId;
    }
}

public class BacktestCompletedEvent : DomainEvent
{
    public string BacktestId { get; init; } = string.Empty;
    public TimeSpan BacktestPeriod { get; init; }
    public double OverallPerformance { get; init; }
    public int TotalTrades { get; init; }
    public double WinRate { get; init; }
    public Dictionary<string, object> PerformanceMetrics { get; init; } = new();

    public BacktestCompletedEvent() { }

    public BacktestCompletedEvent(string backtestId) : base(backtestId)
    {
        BacktestId = backtestId;
    }
}