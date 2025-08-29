using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using MedjCap.Data.Infrastructure.CQRS.Commands.Handlers;
using MedjCap.Data.Infrastructure.CQRS.Queries.Handlers;
using MedjCap.Data.TimeSeries.Commands;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.Statistics.Models;
using MedjCap.Data.Statistics.Correlation.Models;
using MedjCap.Data.Analysis.Models;

namespace MedjCap.Data.Infrastructure.CQRS.Core;

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
        services.AddTransient<ICommandHandler<AddDataPointCommand>, AddDataPointCommandHandler>();
        services.AddTransient<ICommandHandler<AddMultipleDataPointCommand>, AddMultipleDataPointCommandHandler>();
        services.AddTransient<ICommandHandler<ClearDataCommand>, ClearDataCommandHandler>();

        // Register data retrieval query handlers with their interfaces
        services.AddTransient<IQueryHandler<Queries.GetDataPointsQuery, IEnumerable<DataPoint>>, GetDataPointsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetMultiDataPointsQuery, IEnumerable<MultiDataPoint>>, GetMultiDataPointsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataByMeasurementIdQuery, IEnumerable<DataPoint>>, GetDataByMeasurementIdQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataByDateRangeQuery, IEnumerable<DataPoint>>, GetDataByDateRangeQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetTimeSeriesDataQuery, TimeSeriesData>, GetTimeSeriesDataQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataStatisticsQuery, DataStatistics>, GetDataStatisticsQueryHandler>();

        // Register correlation analysis query handlers with their interfaces
        services.AddTransient<IQueryHandler<Queries.CalculatePriceMovementsQuery, List<PriceMovement>>, CalculatePriceMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateMultipleTimeHorizonPriceMovementsQuery, Dictionary<TimeSpan, List<PriceMovement>>>, CalculateMultipleTimeHorizonPriceMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateCorrelationQuery, CorrelationResult>, CalculateCorrelationQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.BucketizeMovementsQuery, Dictionary<string, List<PriceMovement>>>, BucketizeMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.AnalyzeByMeasurementRangesQuery, Dictionary<string, RangeAnalysisResult>>, AnalyzeByMeasurementRangesQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateCorrelationWithContextualFilterQuery, CorrelationResult>, CalculateCorrelationWithContextualFilterQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.RunFullCorrelationAnalysisQuery, CorrelationAnalysisResult>, RunFullCorrelationAnalysisQueryHandler>();

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
        services.AddTransient<ICommandHandler<AddDataPointCommand>, AddDataPointCommandHandler>();
        services.AddTransient<ICommandHandler<AddMultipleDataPointCommand>, AddMultipleDataPointCommandHandler>();
        services.AddTransient<ICommandHandler<ClearDataCommand>, ClearDataCommandHandler>();
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
        services.AddTransient<IQueryHandler<Queries.GetDataPointsQuery, IEnumerable<DataPoint>>, GetDataPointsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetMultiDataPointsQuery, IEnumerable<MultiDataPoint>>, GetMultiDataPointsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataByMeasurementIdQuery, IEnumerable<DataPoint>>, GetDataByMeasurementIdQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataByDateRangeQuery, IEnumerable<DataPoint>>, GetDataByDateRangeQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetTimeSeriesDataQuery, TimeSeriesData>, GetTimeSeriesDataQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.GetDataStatisticsQuery, DataStatistics>, GetDataStatisticsQueryHandler>();

        // Correlation analysis query handlers
        services.AddTransient<IQueryHandler<Queries.CalculatePriceMovementsQuery, List<PriceMovement>>, CalculatePriceMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateMultipleTimeHorizonPriceMovementsQuery, Dictionary<TimeSpan, List<PriceMovement>>>, CalculateMultipleTimeHorizonPriceMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateCorrelationQuery, CorrelationResult>, CalculateCorrelationQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.BucketizeMovementsQuery, Dictionary<string, List<PriceMovement>>>, BucketizeMovementsQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.AnalyzeByMeasurementRangesQuery, Dictionary<string, RangeAnalysisResult>>, AnalyzeByMeasurementRangesQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.CalculateCorrelationWithContextualFilterQuery, CorrelationResult>, CalculateCorrelationWithContextualFilterQueryHandler>();
        services.AddTransient<IQueryHandler<Queries.RunFullCorrelationAnalysisQuery, CorrelationAnalysisResult>, RunFullCorrelationAnalysisQueryHandler>();

        return services;
    }
}