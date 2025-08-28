using MedjCap.Data.Domain;

namespace MedjCap.Data.Core;

/// <summary>
/// Abstraction for data storage operations
/// Allows switching between in-memory, file-based, database, or other storage implementations
/// </summary>
public interface IDataStorage<T> where T : class
{
    Task SaveAsync(T item, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> QueryAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized data storage interface for time-series data points
/// </summary>
public interface ITimeSeriesDataStorage : IDataStorage<DataPoint>
{
    Task<IEnumerable<DataPoint>> GetByMeasurementIdAsync(string measurementId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DataPoint>> GetByTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<IEnumerable<DataPoint>> GetByMeasurementAndTimeRangeAsync(string measurementId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<TimeSeriesData> GetTimeSeriesDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized data storage interface for multi-measurement data points  
/// </summary>
public interface IMultiDataStorage : IDataStorage<MultiDataPoint>
{
    Task<IEnumerable<MultiDataPoint>> GetByTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<IEnumerable<MultiDataPoint>> GetContainingMeasurementAsync(string measurementId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized data storage interface for analysis results
/// </summary>
public interface IAnalysisStorage : IDataStorage<AnalysisResult>
{
    Task<IEnumerable<AnalysisResult>> GetByMeasurementIdAsync(string measurementId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AnalysisResult>> GetHistoryAsync(string measurementId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized data storage interface for configuration management
/// </summary>
public interface IConfigurationStorage : IDataStorage<AnalysisConfig>
{
    Task SaveConfigurationAsync(AnalysisConfig config, string configurationName, CancellationToken cancellationToken = default);
    Task<AnalysisConfig?> GetConfigurationAsync(string configurationName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetConfigurationNamesAsync(CancellationToken cancellationToken = default);
}