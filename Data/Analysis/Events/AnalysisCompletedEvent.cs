using System;
using MedjCap.Data.Infrastructure.Events.Core;

namespace MedjCap.Data.Analysis.Events;

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