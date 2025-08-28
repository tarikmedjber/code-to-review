using System.Collections.Concurrent;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;

namespace MedjCap.Data.Storage;

/// <summary>
/// In-memory implementation of analysis storage
/// Thread-safe and suitable for development/testing environments
/// </summary>
public class InMemoryAnalysisStorage : IAnalysisStorage
{
    private readonly ConcurrentDictionary<string, AnalysisResult> _analysisResults = new();

    public Task SaveAsync(AnalysisResult item, CancellationToken cancellationToken = default)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        
        _analysisResults.AddOrUpdate(item.AnalysisId, item, (key, existing) => item);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<AnalysisResult> items, CancellationToken cancellationToken = default)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        foreach (var item in items)
        {
            _analysisResults.AddOrUpdate(item.AnalysisId, item, (key, existing) => item);
        }
        
        return Task.CompletedTask;
    }

    public Task<AnalysisResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        _analysisResults.TryGetValue(id, out var result);
        return Task.FromResult(result);
    }

    public Task<IEnumerable<AnalysisResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = _analysisResults.Values.ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<AnalysisResult>> QueryAsync(Func<AnalysisResult, bool> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var result = _analysisResults.Values.Where(predicate).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        var exists = _analysisResults.ContainsKey(id);
        return Task.FromResult(exists);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        var removed = _analysisResults.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _analysisResults.Clear();
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AnalysisResult>> GetByMeasurementIdAsync(string measurementId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(measurementId)) throw new ArgumentNullException(nameof(measurementId));
        
        var result = _analysisResults.Values
            .Where(ar => ar.MeasurementId == measurementId)
            .OrderByDescending(ar => ar.CompletedAt)
            .ToList()
            .AsEnumerable();
        
        return Task.FromResult(result);
    }

    public Task<IEnumerable<AnalysisResult>> GetHistoryAsync(string measurementId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(measurementId)) throw new ArgumentNullException(nameof(measurementId));
        
        var query = _analysisResults.Values.Where(r => r.MeasurementId == measurementId);
        
        if (from.HasValue)
            query = query.Where(r => r.CompletedAt >= from.Value);
            
        if (to.HasValue)
            query = query.Where(r => r.CompletedAt <= to.Value);
        
        var result = query
            .OrderByDescending(r => r.CompletedAt)
            .ToList()
            .AsEnumerable();
        
        return Task.FromResult(result);
    }
}