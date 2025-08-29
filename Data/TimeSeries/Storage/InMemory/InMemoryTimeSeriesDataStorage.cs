using System.Collections.Concurrent;
using MedjCap.Data.Infrastructure.Interfaces;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.DataQuality.Interfaces;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.TimeSeries.Models;

namespace MedjCap.Data.TimeSeries.Storage.InMemory;

/// <summary>
/// In-memory implementation of time-series data storage
/// Thread-safe and suitable for development/testing environments
/// </summary>
public class InMemoryTimeSeriesDataStorage : ITimeSeriesDataStorage
{
    private readonly ConcurrentBag<DataPoint> _dataPoints = new();
    private readonly object _lockObject = new();

    public Task SaveAsync(DataPoint item, CancellationToken cancellationToken = default)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        
        // Generate ID if not present
        if (string.IsNullOrEmpty(item.Id))
        {
            item.Id = GenerateId(item);
        }
        
        _dataPoints.Add(item);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<DataPoint> items, CancellationToken cancellationToken = default)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = GenerateId(item);
            }
            _dataPoints.Add(item);
        }
        
        return Task.CompletedTask;
    }

    public Task<DataPoint?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        var result = _dataPoints.FirstOrDefault(dp => dp.Id == id);
        return Task.FromResult(result);
    }

    public Task<IEnumerable<DataPoint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = _dataPoints
            .OrderBy(dp => dp.Timestamp)
            .ToList()
            .AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<DataPoint>> QueryAsync(Func<DataPoint, bool> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var result = _dataPoints.Where(predicate).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        var exists = _dataPoints.Any(dp => dp.Id == id);
        return Task.FromResult(exists);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        // Note: ConcurrentBag doesn't support removal, so this is a limitation of in-memory storage
        // In a real implementation, you'd use a different collection or implement a soft delete
        throw new NotSupportedException("In-memory storage does not support deletion. Use ClearAsync() to clear all data.");
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            // Clear by recreating the collection - not ideal but works for in-memory
            while (_dataPoints.TryTake(out _))
            {
                // Remove all items
            }
        }
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<DataPoint>> GetByMeasurementIdAsync(string measurementId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(measurementId)) throw new ArgumentNullException(nameof(measurementId));
        
        var result = _dataPoints
            .Where(dp => dp.MeasurementId == measurementId)
            .OrderBy(dp => dp.Timestamp)
            .ToList()
            .AsEnumerable();
        
        return Task.FromResult(result);
    }

    public Task<IEnumerable<DataPoint>> GetByTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var result = _dataPoints
            .Where(dp => dp.Timestamp >= start && dp.Timestamp <= end)
            .OrderBy(dp => dp.Timestamp)
            .ToList()
            .AsEnumerable();
        
        return Task.FromResult(result);
    }

    public Task<IEnumerable<DataPoint>> GetByMeasurementAndTimeRangeAsync(string measurementId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(measurementId)) throw new ArgumentNullException(nameof(measurementId));
        
        var result = _dataPoints
            .Where(dp => dp.MeasurementId == measurementId && dp.Timestamp >= start && dp.Timestamp <= end)
            .OrderBy(dp => dp.Timestamp)
            .ToList()
            .AsEnumerable();
        
        return Task.FromResult(result);
    }

    public Task<TimeSeriesData> GetTimeSeriesDataAsync(CancellationToken cancellationToken = default)
    {
        var dataPoints = _dataPoints
            .OrderBy(dp => dp.Timestamp)
            .ToList();
        
        if (dataPoints.Count < 2)
        {
            var timeSeries = new TimeSeriesData
            {
                DataPoints = dataPoints,
                TimeStep = TimeSpan.Zero,
                IsRegular = dataPoints.Count <= 1
            };
            return Task.FromResult(timeSeries);
        }

        // Calculate intervals between consecutive timestamps
        var intervals = new List<TimeSpan>();
        for (int i = 1; i < dataPoints.Count; i++)
        {
            intervals.Add(dataPoints[i].Timestamp - dataPoints[i - 1].Timestamp);
        }

        // Detect if all intervals are the same (regular time series)
        var firstInterval = intervals[0];
        var isRegular = intervals.All(interval => interval == firstInterval);

        var result = new TimeSeriesData
        {
            DataPoints = dataPoints,
            TimeStep = firstInterval,
            IsRegular = isRegular
        };
        
        return Task.FromResult(result);
    }

    /// <summary>
    /// Generates a unique ID for a data point based on timestamp and measurement ID
    /// </summary>
    private static string GenerateId(DataPoint dataPoint)
    {
        return $"{dataPoint.MeasurementId}_{dataPoint.Timestamp:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
    }
}