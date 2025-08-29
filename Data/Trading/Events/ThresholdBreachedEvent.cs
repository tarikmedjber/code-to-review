using System;
using System.Collections.Generic;
using MedjCap.Data.Infrastructure.Events.Core;

namespace MedjCap.Data.Trading.Events;

public enum ThresholdDirection
{
    Above,
    Below
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