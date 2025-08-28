using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using MedjCap.Data.CQRS;
using MedjCap.Data.CQRS.Commands.Handlers;
using MedjCap.Data.CQRS.Queries.Handlers;

namespace MedjCap.Data.CQRS;

/// <summary>
/// Extension methods for registering CQRS components in the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all CQRS components including dispatchers and handlers.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCQRS(this IServiceCollection services)
    {
        // Register dispatchers
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IQueryDispatcher, QueryDispatcher>();

        // Register command handlers with their interfaces
        services.AddTransient<ICommandHandler<Commands.AddDataPointCommand>, AddDataPointCommandHandler>();
        services.AddTransient<ICommandHandler<Commands.AddMultipleDataPointCommand>, AddMultipleDataPointCommandHandler>();
        services.AddTransient<ICommandHandler<Commands.ClearDataCommand>, ClearDataCommandHandler>();

        // Register data retrieval query handlers with their interfaces
        services.AddTransient<IQueryHandler<Queries.GetDataPointsQuery, IEnumerable<Domain.DataPoint>>, GetDataPointsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetMultiDataPointsQuery, IEnumerable<Domain.MultiDataPoint>>, GetMultiDataPointsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataByMeasurementIdQuery, IEnumerable<Domain.DataPoint>>, GetDataByMeasurementIdQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataByDateRangeQuery, IEnumerable<Domain.DataPoint>>, GetDataByDateRangeQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetTimeSeriesDataQuery, Domain.TimeSeriesData>, GetTimeSeriesDataQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataStatisticsQuery, Domain.DataStatistics>, GetDataStatisticsQueryHandler>();

        // Register correlation analysis query handlers with their interfaces
        services.AddTransient<IQueryHandler<Queries.CalculatePriceMovementsQuery, List<Domain.PriceMovement>>, CalculatePriceMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateMultipleTimeHorizonPriceMovementsQuery, Dictionary<TimeSpan, List<Domain.PriceMovement>>>, CalculateMultipleTimeHorizonPriceMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateCorrelationQuery, Domain.CorrelationResult>, CalculateCorrelationQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.BucketizeMovementsQuery, Dictionary<string, List<Domain.PriceMovement>>>, BucketizeMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.AnalyzeByMeasurementRangesQuery, Dictionary<string, Domain.RangeAnalysisResult>>, AnalyzeByMeasurementRangesQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateCorrelationWithContextualFilterQuery, Domain.CorrelationResult>, CalculateCorrelationWithContextualFilterQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.RunFullCorrelationAnalysisQuery, Domain.CorrelationAnalysisResult>, RunFullCorrelationAnalysisQueryHandler>();

        return services;
    }

    /// <summary>
    /// Registers CQRS dispatchers only (without handlers).
    /// Use this if you want to register handlers manually or selectively.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCQRSDispatchers(this IServiceCollection services)
    {
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
        return services;
    }

    /// <summary>
    /// Registers all command handlers.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<Commands.AddDataPointCommand>, AddDataPointCommandHandler>();
        services.AddTransient<ICommandHandler<Commands.AddMultipleDataPointCommand>, AddMultipleDataPointCommandHandler>();
        services.AddTransient<ICommandHandler<Commands.ClearDataCommand>, ClearDataCommandHandler>();
        return services;
    }

    /// <summary>
    /// Registers all query handlers.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddQueryHandlers(this IServiceCollection services)
    {
        // Data retrieval query handlers
        services.AddTransient<IQueryHandler<Queries.GetDataPointsQuery, IEnumerable<Domain.DataPoint>>, GetDataPointsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetMultiDataPointsQuery, IEnumerable<Domain.MultiDataPoint>>, GetMultiDataPointsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataByMeasurementIdQuery, IEnumerable<Domain.DataPoint>>, GetDataByMeasurementIdQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataByDateRangeQuery, IEnumerable<Domain.DataPoint>>, GetDataByDateRangeQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetTimeSeriesDataQuery, Domain.TimeSeriesData>, GetTimeSeriesDataQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataStatisticsQuery, Domain.DataStatistics>, GetDataStatisticsQueryHandler>();

        // Correlation analysis query handlers
        services.AddTransient<IQueryHandler<Queries.CalculatePriceMovementsQuery, List<Domain.PriceMovement>>, CalculatePriceMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateMultipleTimeHorizonPriceMovementsQuery, Dictionary<TimeSpan, List<Domain.PriceMovement>>>, CalculateMultipleTimeHorizonPriceMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateCorrelationQuery, Domain.CorrelationResult>, CalculateCorrelationQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.BucketizeMovementsQuery, Dictionary<string, List<Domain.PriceMovement>>>, BucketizeMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.AnalyzeByMeasurementRangesQuery, Dictionary<string, Domain.RangeAnalysisResult>>, AnalyzeByMeasurementRangesQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateCorrelationWithContextualFilterQuery, Domain.CorrelationResult>, CalculateCorrelationWithContextualFilterQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.RunFullCorrelationAnalysisQuery, Domain.CorrelationAnalysisResult>, RunFullCorrelationAnalysisQueryHandler>();

        return services;
    }
}