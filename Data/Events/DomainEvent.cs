using System;
using System.Collections.Generic;

namespace MedjCap.Data.Events;

public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string AggregateId { get; protected set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; } = new();

    protected DomainEvent()
    {
    }

    protected DomainEvent(string aggregateId)
    {
        AggregateId = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
    }
}