using System;
using System.Threading.Tasks;

namespace MedjCap.Data.Infrastructure.Events.Core;

public interface IEventDispatcher
{
    Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : DomainEvent;
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : DomainEvent;
    void Subscribe<TEvent>(Func<TEvent, Task> asyncHandler) where TEvent : DomainEvent;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : DomainEvent;
    void Unsubscribe<TEvent>(Func<TEvent, Task> asyncHandler) where TEvent : DomainEvent;
    void ClearSubscriptions<TEvent>() where TEvent : DomainEvent;
    void ClearAllSubscriptions();
}