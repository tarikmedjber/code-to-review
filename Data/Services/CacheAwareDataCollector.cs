using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;

namespace MedjCap.Data.Services;

/// <summary>
/// Data collector wrapper that triggers cache invalidation when data changes.
/// Ensures cached analysis results stay fresh when new data is added.
/// </summary>
public class CacheAwareDataCollector : IDataCollector
{
    private readonly IDataCollector _innerDataCollector;
    private readonly ICacheInvalidationService? _invalidationService;
    private readonly ILogger<CacheAwareDataCollector> _logger;

    public CacheAwareDataCollector(
        IDataCollector innerDataCollector,
        ICacheInvalidationService? invalidationService,
        ILogger<CacheAwareDataCollector> logger)
    {
        _innerDataCollector = innerDataCollector ?? throw new ArgumentNullException(nameof(innerDataCollector));
        _invalidationService = invalidationService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Data Addition Methods - implementing IDataCollector interface
    public void AddDataPoint(DateTime timestamp, string measurementId, decimal measurementValue, decimal price, decimal atr)
    {
        // Add the data point first
        _innerDataCollector.AddDataPoint(timestamp, measurementId, measurementValue, price, atr);

        // Trigger cache invalidation
        if (_invalidationService != null)
        {
            var dataPoint = new DataPoint
            {
                Timestamp = timestamp,
                MeasurementId = measurementId,
                MeasurementValue = measurementValue,
                Price = price,
                ATR = atr
            };

            _invalidationService.OnDataPointAdded(dataPoint);
            _logger.LogDebug("Triggered cache invalidation for new data point: {MeasurementId} at {Timestamp}",
                measurementId, timestamp);
        }
    }

    public void AddDataPoint(DateTime timestamp, string measurementId, decimal measurementValue, decimal price, decimal atr, Dictionary<string, decimal>? contextualData)
    {
        // Add the data point first
        _innerDataCollector.AddDataPoint(timestamp, measurementId, measurementValue, price, atr, contextualData);

        // Trigger cache invalidation
        if (_invalidationService != null)
        {
            var dataPoint = new DataPoint
            {
                Timestamp = timestamp,
                MeasurementId = measurementId,
                MeasurementValue = measurementValue,
                Price = price,
                ATR = atr
            };

            _invalidationService.OnDataPointAdded(dataPoint);
            _logger.LogDebug("Triggered cache invalidation for new contextual data point: {MeasurementId} at {Timestamp}",
                measurementId, timestamp);
        }
    }

    public void AddMultipleDataPoint(DateTime timestamp, Dictionary<string, decimal> measurements, decimal price, decimal atr)
    {
        // Add the multi-measurement data point first
        _innerDataCollector.AddMultipleDataPoint(timestamp, measurements, price, atr);

        // Trigger cache invalidation for all affected measurements
        if (_invalidationService != null)
        {
            foreach (var measurementId in measurements.Keys)
            {
                var dataPoint = new DataPoint
                {
                    Timestamp = timestamp,
                    MeasurementId = measurementId,
                    MeasurementValue = measurements[measurementId],
                    Price = price,
                    ATR = atr
                };

                _invalidationService.OnDataPointAdded(dataPoint);
            }

            _logger.LogDebug("Triggered cache invalidation for multi-measurement data point: {Count} measurements at {Timestamp}",
                measurements.Count, timestamp);
        }
    }

    public void AddMultipleDataPoint(DateTime timestamp, Dictionary<string, decimal> measurements, decimal price, decimal atr, Dictionary<string, decimal>? contextualData)
    {
        // Add the multi-measurement data point first
        _innerDataCollector.AddMultipleDataPoint(timestamp, measurements, price, atr, contextualData);

        // Trigger cache invalidation for all affected measurements
        if (_invalidationService != null)
        {
            foreach (var measurementId in measurements.Keys)
            {
                var dataPoint = new DataPoint
                {
                    Timestamp = timestamp,
                    MeasurementId = measurementId,
                    MeasurementValue = measurements[measurementId],
                    Price = price,
                    ATR = atr
                };

                _invalidationService.OnDataPointAdded(dataPoint);
            }

            _logger.LogDebug("Triggered cache invalidation for contextual multi-measurement data point: {Count} measurements at {Timestamp}",
                measurements.Count, timestamp);
        }
    }

    // Data Retrieval Methods - implementing IDataCollector interface
    public IEnumerable<DataPoint> GetDataPoints()
    {
        return _innerDataCollector.GetDataPoints();
    }

    public IEnumerable<MultiDataPoint> GetMultiDataPoints()
    {
        return _innerDataCollector.GetMultiDataPoints();
    }

    public IEnumerable<DataPoint> GetDataByMeasurementId(string measurementId)
    {
        return _innerDataCollector.GetDataByMeasurementId(measurementId);
    }

    public IEnumerable<DataPoint> GetDataByDateRange(DateRange dateRange)
    {
        return _innerDataCollector.GetDataByDateRange(dateRange);
    }

    // Analysis Support Methods - implementing IDataCollector interface
    public TimeSeriesData GetTimeSeriesData()
    {
        return _innerDataCollector.GetTimeSeriesData();
    }

    public DataStatistics GetStatistics()
    {
        return _innerDataCollector.GetStatistics();
    }

    // Utility Methods - implementing IDataCollector interface
    public void Clear()
    {
        _innerDataCollector.Clear();

        // Invalidate all cache entries since all data was cleared
        if (_invalidationService != null)
        {
            _invalidationService.InvalidateByPattern("", InvalidationScope.Contains);
            _logger.LogInformation("Triggered full cache invalidation due to data clearing");
        }
    }
}