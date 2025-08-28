using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedjCap.Data.Core;
using MedjCap.Data.CQRS;
using MedjCap.Data.CQRS.Queries;
using MedjCap.Data.Domain;

namespace MedjCap.Data.CQRS.Queries.Handlers;

/// <summary>
/// Query handler for retrieving all data points.
/// </summary>
public class GetDataPointsQueryHandler : IQueryHandler<GetDataPointsQuery, IEnumerable<DataPoint>>
{
    private readonly IDataCollector _dataCollector;
    private readonly ILogger<GetDataPointsQueryHandler> _logger;

    public GetDataPointsQueryHandler(IDataCollector dataCollector, ILogger<GetDataPointsQueryHandler> logger)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DataPoint>> HandleAsync(GetDataPointsQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _dataCollector.GetDataPoints();
            _logger.LogDebug("Retrieved all data points via query with ID {QueryId}", query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all data points via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for retrieving all multi-measurement data points.
/// </summary>
public class GetMultiDataPointsQueryHandler : IQueryHandler<GetMultiDataPointsQuery, IEnumerable<MultiDataPoint>>
{
    private readonly IDataCollector _dataCollector;
    private readonly ILogger<GetMultiDataPointsQueryHandler> _logger;

    public GetMultiDataPointsQueryHandler(IDataCollector dataCollector, ILogger<GetMultiDataPointsQueryHandler> logger)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<MultiDataPoint>> HandleAsync(GetMultiDataPointsQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _dataCollector.GetMultiDataPoints();
            _logger.LogDebug("Retrieved all multi-data points via query with ID {QueryId}", query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve multi-data points via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for retrieving data points by measurement ID.
/// </summary>
public class GetDataByMeasurementIdQueryHandler : IQueryHandler<GetDataByMeasurementIdQuery, IEnumerable<DataPoint>>
{
    private readonly IDataCollector _dataCollector;
    private readonly ILogger<GetDataByMeasurementIdQueryHandler> _logger;

    public GetDataByMeasurementIdQueryHandler(IDataCollector dataCollector, ILogger<GetDataByMeasurementIdQueryHandler> logger)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DataPoint>> HandleAsync(GetDataByMeasurementIdQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _dataCollector.GetDataByMeasurementId(query.MeasurementId);
            _logger.LogDebug("Retrieved data points for measurement {MeasurementId} via query with ID {QueryId}", 
                query.MeasurementId, query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve data points for measurement {MeasurementId} via query with ID {QueryId}", 
                query.MeasurementId, query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for retrieving data points by date range.
/// </summary>
public class GetDataByDateRangeQueryHandler : IQueryHandler<GetDataByDateRangeQuery, IEnumerable<DataPoint>>
{
    private readonly IDataCollector _dataCollector;
    private readonly ILogger<GetDataByDateRangeQueryHandler> _logger;

    public GetDataByDateRangeQueryHandler(IDataCollector dataCollector, ILogger<GetDataByDateRangeQueryHandler> logger)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DataPoint>> HandleAsync(GetDataByDateRangeQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _dataCollector.GetDataByDateRange(query.DateRange);
            _logger.LogDebug("Retrieved data points for date range {Start} to {End} via query with ID {QueryId}", 
                query.DateRange.Start, query.DateRange.End, query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve data points for date range via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for retrieving time series data.
/// </summary>
public class GetTimeSeriesDataQueryHandler : IQueryHandler<GetTimeSeriesDataQuery, TimeSeriesData>
{
    private readonly IDataCollector _dataCollector;
    private readonly ILogger<GetTimeSeriesDataQueryHandler> _logger;

    public GetTimeSeriesDataQueryHandler(IDataCollector dataCollector, ILogger<GetTimeSeriesDataQueryHandler> logger)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TimeSeriesData> HandleAsync(GetTimeSeriesDataQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _dataCollector.GetTimeSeriesData();
            _logger.LogDebug("Retrieved time series data via query with ID {QueryId}", query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve time series data via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for retrieving data statistics.
/// </summary>
public class GetDataStatisticsQueryHandler : IQueryHandler<GetDataStatisticsQuery, DataStatistics>
{
    private readonly IDataCollector _dataCollector;
    private readonly ILogger<GetDataStatisticsQueryHandler> _logger;

    public GetDataStatisticsQueryHandler(IDataCollector dataCollector, ILogger<GetDataStatisticsQueryHandler> logger)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DataStatistics> HandleAsync(GetDataStatisticsQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _dataCollector.GetStatistics();
            _logger.LogDebug("Retrieved data statistics via query with ID {QueryId}", query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve data statistics via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}