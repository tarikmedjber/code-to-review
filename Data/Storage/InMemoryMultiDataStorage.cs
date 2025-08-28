using System.Collections.Concurrent;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;

namespace MedjCap.Data.Storage;

/// <summary>
/// In-memory implementation of multi-data storage
/// Thread-safe and suitable for development/testing environments
/// </summary>
public class InMemoryMultiDataStorage : IMultiDataStorage
{
    private readonly ConcurrentBag<MultiDataPoint> _multiDataPoints = new();
    private readonly object _lockObject = new();

    public Task SaveAsync(MultiDataPoint item, CancellationToken cancellationToken = default)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        
        // Generate ID if not present
        if (string.IsNullOrEmpty(item.Id))
        {
            item.Id = GenerateId(item);
        }
        
        _multiDataPoints.Add(item);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<MultiDataPoint> items, CancellationToken cancellationToken = default)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = GenerateId(item);
            }
            _multiDataPoints.Add(item);
        }
        
        return Task.CompletedTask;
    }

    public Task<MultiDataPoint?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        var result = _multiDataPoints.FirstOrDefault(mdp => mdp.Id == id);
        return Task.FromResult(result);
    }

    public Task<IEnumerable<MultiDataPoint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = _multiDataPoints.OrderBy(mdp => mdp.Timestamp).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<MultiDataPoint>> QueryAsync(Func<MultiDataPoint, bool> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var result = _multiDataPoints.Where(predicate).OrderBy(mdp => mdp.Timestamp).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        var exists = _multiDataPoints.Any(mdp => mdp.Id == id);
        return Task.FromResult(exists);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        // Note: ConcurrentBag doesn't support removal, so this is a limitation of in-memory storage
        throw new NotSupportedException("In-memory storage does not support deletion. Use ClearAsync() to clear all data.");
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            // Clear by removing all items
            while (_multiDataPoints.TryTake(out _))
            {
                // Remove all items
            }
        }
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<MultiDataPoint>> GetByTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var result = _multiDataPoints
            .Where(mdp => mdp.Timestamp >= start && mdp.Timestamp <= end)
            .OrderBy(mdp => mdp.Timestamp)
            .ToList()
            .AsEnumerable();
        
        return Task.FromResult(result);
    }

    public Task<IEnumerable<MultiDataPoint>> GetContainingMeasurementAsync(string measurementId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(measurementId)) throw new ArgumentNullException(nameof(measurementId));
        
        var result = _multiDataPoints
            .Where(mdp => mdp.Measurements.ContainsKey(measurementId))
            .OrderBy(mdp => mdp.Timestamp)
            .ToList()
            .AsEnumerable();
        
        return Task.FromResult(result);
    }

    /// <summary>
    /// Generates a unique ID for a multi-data point based on timestamp
    /// </summary>
    private static string GenerateId(MultiDataPoint multiDataPoint)
    {
        return $"multi_{multiDataPoint.Timestamp:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
    }
}