using System;
using MedjCap.Data.Infrastructure.Events.Core;

namespace MedjCap.Data.MachineLearning.Events;

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