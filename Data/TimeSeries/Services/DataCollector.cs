using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.Statistics.Models;
using MedjCap.Data.Trading.Models;

namespace MedjCap.Data.TimeSeries.Services;

/// <summary>
/// Implementation of IDataCollector for collecting time-series data points.
/// Uses abstracted storage layer for persistence, allowing easy swapping of storage implementations.
/// </summary>
public class DataCollector : IDataCollector
{
    private readonly ITimeSeriesDataStorage _timeSeriesStorage;
    private readonly IMultiDataStorage _multiDataStorage;
    
    public DataCollector(ITimeSeriesDataStorage timeSeriesStorage, IMultiDataStorage multiDataStorage)
    {
        _timeSeriesStorage = timeSeriesStorage ?? throw new ArgumentNullException(nameof(timeSeriesStorage));
        _multiDataStorage = multiDataStorage ?? throw new ArgumentNullException(nameof(multiDataStorage));
    }
    // Data Addition Methods
    public void AddDataPoint(DateTime timestamp, string measurementId, decimal measurementValue, decimal price, decimal atr)
    {
        AddDataPoint(timestamp, measurementId, measurementValue, price, atr, null);
    }

    public void AddDataPoint(DateTime timestamp, string measurementId, decimal measurementValue, decimal price, decimal atr, Dictionary<string, decimal>? contextualData)
    {
        if (string.IsNullOrWhiteSpace(measurementId))
            throw new ArgumentException("MeasurementId cannot be null or empty", nameof(measurementId));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative");
        if (atr < 0)
            throw new ArgumentOutOfRangeException(nameof(atr), "ATR cannot be negative");
        var dataPoint = new DataPoint
        {
            Timestamp = timestamp,
            MeasurementId = measurementId,
            MeasurementValue = measurementValue,
            Price = price,
            ATR = atr,
            ContextualData = contextualData ?? new Dictionary<string, decimal>()
        };
        // Note: Using sync over async for backward compatibility
        // In a production system, this would be async all the way up
        _timeSeriesStorage.SaveAsync(dataPoint).GetAwaiter().GetResult();
    }

    public void AddMultipleDataPoint(DateTime timestamp, Dictionary<string, decimal> measurements, decimal price, decimal atr)
    {
        AddMultipleDataPoint(timestamp, measurements, price, atr, null);
    }

    public void AddMultipleDataPoint(DateTime timestamp, Dictionary<string, decimal> measurements, decimal price, decimal atr, Dictionary<string, decimal>? contextualData)
    {
        if (measurements == null)
            throw new ArgumentNullException(nameof(measurements));
        if (measurements.Count == 0)
            throw new ArgumentException("Measurements dictionary cannot be empty", nameof(measurements));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative");
        if (atr < 0)
            throw new ArgumentOutOfRangeException(nameof(atr), "ATR cannot be negative");
        var multiDataPoint = new MultiDataPoint
        {
            Timestamp = timestamp,
            Measurements = measurements ?? new Dictionary<string, decimal>(),
            Price = price,
            ATR = atr,
            ContextualData = contextualData ?? new Dictionary<string, decimal>()
        };
        // Note: Using sync over async for backward compatibility
        _multiDataStorage.SaveAsync(multiDataPoint).GetAwaiter().GetResult();
    }

    // Data Retrieval Methods
    public IEnumerable<DataPoint> GetDataPoints()
    {
        // Note: Using sync over async for backward compatibility
        return _timeSeriesStorage.GetAllAsync().GetAwaiter().GetResult();
    }

    public IEnumerable<MultiDataPoint> GetMultiDataPoints()
    {
        // Note: Using sync over async for backward compatibility
        return _multiDataStorage.GetAllAsync().GetAwaiter().GetResult();
    }

    public IEnumerable<DataPoint> GetDataByMeasurementId(string measurementId)
    {
        if (string.IsNullOrWhiteSpace(measurementId))
            throw new ArgumentException("MeasurementId cannot be null or empty", nameof(measurementId));
        // Note: Using sync over async for backward compatibility
        return _timeSeriesStorage.GetByMeasurementIdAsync(measurementId).GetAwaiter().GetResult();
    }

    public IEnumerable<DataPoint> GetDataByDateRange(DateRange dateRange)
    {
        if (dateRange == null)
            throw new ArgumentNullException(nameof(dateRange));
        if (dateRange.Start > dateRange.End)
            throw new ArgumentException("Start date must be before end date", nameof(dateRange));
        // Note: Using sync over async for backward compatibility
        return _timeSeriesStorage.GetByTimeRangeAsync(dateRange.Start, dateRange.End).GetAwaiter().GetResult();
    }

    // Analysis Support Methods
    public TimeSeriesData GetTimeSeriesData()
    {
        // Note: Using sync over async for backward compatibility
        return _timeSeriesStorage.GetTimeSeriesDataAsync().GetAwaiter().GetResult();
    }

    // Legacy method - kept for backward compatibility but implemented using storage
    private TimeSeriesData GetTimeSeriesDataLegacy()
    {
        var orderedData = GetDataPoints().ToList();
        
        if (orderedData.Count < 2)
        {
            return new TimeSeriesData
            {
                DataPoints = orderedData,
                TimeStep = TimeSpan.Zero,
                IsRegular = orderedData.Count <= 1
            };
        }

        // Calculate intervals between consecutive timestamps
        var intervals = new List<TimeSpan>();
        for (int i = 1; i < orderedData.Count; i++)
        {
            intervals.Add(orderedData[i].Timestamp - orderedData[i - 1].Timestamp);
        }

        // Detect if all intervals are the same (regular time series)
        var firstInterval = intervals[0];
        var isRegular = intervals.All(interval => interval == firstInterval);

        return new TimeSeriesData
        {
            DataPoints = orderedData,
            TimeStep = firstInterval,
            IsRegular = isRegular
        };
    }

    public DataStatistics GetStatistics()
    {
        // Note: Using sync over async for backward compatibility
        var dataPoints = _timeSeriesStorage.GetAllAsync().GetAwaiter().GetResult().ToList();
        
        if (!dataPoints.Any())
        {
            return new DataStatistics
            {
                TotalDataPoints = 0,
                UniqueTimestamps = 0,
                UniqueMeasurementIds = Enumerable.Empty<string>(),
                DateRange = new DateRange(),
                PriceRange = new PriceRange()
            };
        }

        var orderedData = dataPoints.OrderBy(dp => dp.Timestamp).ToList();
        var uniqueTimestamps = dataPoints.Select(dp => dp.Timestamp).Distinct().Count();
        var uniqueMeasurementIds = dataPoints.Select(dp => dp.MeasurementId).Distinct().ToList();
        
        var minTimestamp = orderedData.First().Timestamp;
        var maxTimestamp = orderedData.Last().Timestamp;
        
        var minPrice = dataPoints.Min(dp => dp.Price);
        var maxPrice = dataPoints.Max(dp => dp.Price);

        return new DataStatistics
        {
            TotalDataPoints = dataPoints.Count,
            UniqueTimestamps = uniqueTimestamps,
            UniqueMeasurementIds = uniqueMeasurementIds,
            DateRange = new DateRange { Start = minTimestamp, End = maxTimestamp },
            PriceRange = new PriceRange { Min = minPrice, Max = maxPrice }
        };
    }

    // Utility Methods
    public void Clear()
    {
        // Note: Using sync over async for backward compatibility
        _timeSeriesStorage.ClearAsync().GetAwaiter().GetResult();
        _multiDataStorage.ClearAsync().GetAwaiter().GetResult();
    }
}