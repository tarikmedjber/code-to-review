using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MedjCap.Data.Infrastructure.Events.Core;

public class InMemoryEventDispatcher : IEventDispatcher
{
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
    private readonly ILogger<InMemoryEventDispatcher> _logger;
    private readonly object _lockObject = new();

    public InMemoryEventDispatcher(ILogger<InMemoryEventDispatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : DomainEvent
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));

        _logger.LogDebug("Publishing event {EventType} with ID {EventId}", 
            typeof(TEvent).Name, domainEvent.EventId);

        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            _logger.LogDebug("No handlers registered for event type {EventType}", typeof(TEvent).Name);
            return;
        }

        var handlersCopy = new List<object>();
        lock (_lockObject)
        {
            handlersCopy.AddRange(handlers);
        }

        var tasks = new List<Task>();
        var errors = new List<Exception>();

        foreach (var handler in handlersCopy)
        {
            try
            {
                if (handler is Action<TEvent> syncHandler)
                {
                    // Execute sync handlers on the task pool to avoid blocking
                    tasks.Add(Task.Run(() => syncHandler(domainEvent)));
                }
                else if (handler is Func<TEvent, Task> asyncHandler)
                {
                    tasks.Add(asyncHandler(domainEvent));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing handler for event {EventType} with ID {EventId}", 
                    typeof(TEvent).Name, domainEvent.EventId);
                errors.Add(ex);
            }
        }

        if (tasks.Any())
        {
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "One or more handlers failed for event {EventType} with ID {EventId}", 
                    typeof(TEvent).Name, domainEvent.EventId);
                errors.Add(ex);
            }
        }

        if (errors.Any())
        {
            _logger.LogWarning("Event {EventType} with ID {EventId} had {ErrorCount} handler errors", 
                typeof(TEvent).Name, domainEvent.EventId, errors.Count);
            // Note: We don't rethrow to avoid disrupting the main flow
            // Errors are logged for monitoring and debugging
        }

        _logger.LogDebug("Completed publishing event {EventType} with ID {EventId}. {HandlerCount} handlers executed.", 
            typeof(TEvent).Name, domainEvent.EventId, handlersCopy.Count);
    }

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : DomainEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        lock (_lockObject)
        {
            var eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<object>();
                _handlers[eventType] = handlers;
            }

            handlers.Add(handler);
        }

        _logger.LogDebug("Subscribed sync handler for event type {EventType}", typeof(TEvent).Name);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> asyncHandler) where TEvent : DomainEvent
    {
        if (asyncHandler == null) throw new ArgumentNullException(nameof(asyncHandler));

        lock (_lockObject)
        {
            var eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<object>();
                _handlers[eventType] = handlers;
            }

            handlers.Add(asyncHandler);
        }

        _logger.LogDebug("Subscribed async handler for event type {EventType}", typeof(TEvent).Name);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : DomainEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        lock (_lockObject)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers.Remove(handler);
                if (!handlers.Any())
                {
                    _handlers.TryRemove(typeof(TEvent), out _);
                }
            }
        }

        _logger.LogDebug("Unsubscribed sync handler for event type {EventType}", typeof(TEvent).Name);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> asyncHandler) where TEvent : DomainEvent
    {
        if (asyncHandler == null) throw new ArgumentNullException(nameof(asyncHandler));

        lock (_lockObject)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers.Remove(asyncHandler);
                if (!handlers.Any())
                {
                    _handlers.TryRemove(typeof(TEvent), out _);
                }
            }
        }

        _logger.LogDebug("Unsubscribed async handler for event type {EventType}", typeof(TEvent).Name);
    }

    public void ClearSubscriptions<TEvent>() where TEvent : DomainEvent
    {
        lock (_lockObject)
        {
            _handlers.TryRemove(typeof(TEvent), out _);
        }

        _logger.LogDebug("Cleared all subscriptions for event type {EventType}", typeof(TEvent).Name);
    }

    public void ClearAllSubscriptions()
    {
        lock (_lockObject)
        {
            _handlers.Clear();
        }

        _logger.LogDebug("Cleared all event subscriptions");
    }
}