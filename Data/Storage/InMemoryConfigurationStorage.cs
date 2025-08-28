using System.Collections.Concurrent;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;

namespace MedjCap.Data.Storage;

/// <summary>
/// In-memory implementation of configuration storage
/// Thread-safe and suitable for development/testing environments
/// </summary>
public class InMemoryConfigurationStorage : IConfigurationStorage
{
    private readonly ConcurrentDictionary<string, AnalysisConfig> _configurations = new();

    public Task SaveAsync(AnalysisConfig item, CancellationToken cancellationToken = default)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        
        // Generate a default name if none provided
        var configName = $"config_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
        _configurations.AddOrUpdate(configName, item, (key, existing) => item);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<AnalysisConfig> items, CancellationToken cancellationToken = default)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        foreach (var item in items)
        {
            var configName = $"config_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
            _configurations.AddOrUpdate(configName, item, (key, existing) => item);
        }
        
        return Task.CompletedTask;
    }

    public Task<AnalysisConfig?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        _configurations.TryGetValue(id, out var result);
        return Task.FromResult(result);
    }

    public Task<IEnumerable<AnalysisConfig>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = _configurations.Values.ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<AnalysisConfig>> QueryAsync(Func<AnalysisConfig, bool> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var result = _configurations.Values.Where(predicate).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        var exists = _configurations.ContainsKey(id);
        return Task.FromResult(exists);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
        var removed = _configurations.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _configurations.Clear();
        return Task.CompletedTask;
    }

    public Task SaveConfigurationAsync(AnalysisConfig config, string configurationName, CancellationToken cancellationToken = default)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrEmpty(configurationName)) throw new ArgumentNullException(nameof(configurationName));
        
        _configurations.AddOrUpdate(configurationName, config, (key, existing) => config);
        return Task.CompletedTask;
    }

    public Task<AnalysisConfig?> GetConfigurationAsync(string configurationName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(configurationName)) throw new ArgumentNullException(nameof(configurationName));
        
        _configurations.TryGetValue(configurationName, out var config);
        return Task.FromResult(config);
    }

    public Task<IEnumerable<string>> GetConfigurationNamesAsync(CancellationToken cancellationToken = default)
    {
        var names = _configurations.Keys.ToList().AsEnumerable();
        return Task.FromResult(names);
    }
}