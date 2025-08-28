using MedjCap.Data.Events.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace MedjCap.Data.Events;

public static class EventServiceExtensions
{
    public static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        // Register event dispatcher
        services.AddSingleton<IEventDispatcher, InMemoryEventDispatcher>();
        
        // Register event handlers
        services.AddTransient<NotificationEventHandler>();
        
        return services;
    }

    public static IServiceProvider ConfigureEventHandlers(this IServiceProvider serviceProvider)
    {
        var dispatcher = serviceProvider.GetRequiredService<IEventDispatcher>();
        var notificationHandler = serviceProvider.GetRequiredService<NotificationEventHandler>();

        // Subscribe handlers to events
        dispatcher.Subscribe<AnalysisCompletedEvent>(notificationHandler.HandleAnalysisCompleted);
        dispatcher.Subscribe<ThresholdBreachedEvent>(notificationHandler.HandleThresholdBreached);
        dispatcher.Subscribe<DataQualityIssueDetectedEvent>(notificationHandler.HandleDataQualityIssue);
        dispatcher.Subscribe<OptimizationCompletedEvent>(notificationHandler.HandleOptimizationCompleted);
        dispatcher.Subscribe<CorrelationDegradationEvent>(notificationHandler.HandleCorrelationDegradation);
        dispatcher.Subscribe<OutlierDetectedEvent>(notificationHandler.HandleOutlierDetected);
        dispatcher.Subscribe<BacktestCompletedEvent>(notificationHandler.HandleBacktestCompleted);

        return serviceProvider;
    }
}