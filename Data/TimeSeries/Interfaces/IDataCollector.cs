using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.Statistics.Models;

namespace MedjCap.Data.TimeSeries.Interfaces;

/// <summary>
/// Interface for collecting and managing time-series data points for statistical analysis.
/// Supports both single and multiple measurement collection with contextual data.
/// </summary>
public interface IDataCollector
{
    // Data Addition Methods
    /// <summary>
    /// Adds a single measurement data point with timestamp, price, and ATR information.
    /// </summary>
    /// <param name="timestamp">Time of the measurement</param>
    /// <param name="measurementId">Unique identifier for the measurement type (e.g., "PriceVa_Score")</param>
    /// <param name="measurementValue">The measurement value</param>
    /// <param name="price">Market price at the time of measurement</param>
    /// <param name="atr">Average True Range at the time of measurement</param>
    void AddDataPoint(DateTime timestamp, string measurementId, decimal measurementValue, decimal price, decimal atr);
    
    /// <summary>
    /// Adds a single measurement data point with additional contextual data for enhanced analysis.
    /// </summary>
    /// <param name="timestamp">Time of the measurement</param>
    /// <param name="measurementId">Unique identifier for the measurement type</param>
    /// <param name="measurementValue">The measurement value</param>
    /// <param name="price">Market price at the time of measurement</param>
    /// <param name="atr">Average True Range at the time of measurement</param>
    /// <param name="contextualData">Additional market context (volume, volatility, etc.)</param>
    void AddDataPoint(DateTime timestamp, string measurementId, decimal measurementValue, decimal price, decimal atr, Dictionary<string, decimal>? contextualData);
    
    /// <summary>
    /// Adds multiple measurements captured at the same timestamp for efficient batch collection.
    /// </summary>
    /// <param name="timestamp">Time of the measurements</param>
    /// <param name="measurements">Dictionary of measurement ID to value pairs</param>
    /// <param name="price">Market price at the time of measurements</param>
    /// <param name="atr">Average True Range at the time of measurements</param>
    void AddMultipleDataPoint(DateTime timestamp, Dictionary<string, decimal> measurements, decimal price, decimal atr);
    
    /// <summary>
    /// Adds multiple measurements with contextual data for comprehensive market state capture.
    /// </summary>
    /// <param name="timestamp">Time of the measurements</param>
    /// <param name="measurements">Dictionary of measurement ID to value pairs</param>
    /// <param name="price">Market price at the time of measurements</param>
    /// <param name="atr">Average True Range at the time of measurements</param>
    /// <param name="contextualData">Additional market context for enhanced analysis</param>
    void AddMultipleDataPoint(DateTime timestamp, Dictionary<string, decimal> measurements, decimal price, decimal atr, Dictionary<string, decimal>? contextualData);

    // Data Retrieval Methods
    /// <summary>
    /// Retrieves all collected single measurement data points in chronological order.
    /// </summary>
    /// <returns>Enumerable of all data points with measurement values, prices, and ATR</returns>
    IEnumerable<DataPoint> GetDataPoints();
    
    /// <summary>
    /// Retrieves all collected multi-measurement data points for batch analysis.
    /// </summary>
    /// <returns>Enumerable of multi-measurement data points with synchronized measurements</returns>
    IEnumerable<MultiDataPoint> GetMultiDataPoints();
    
    /// <summary>
    /// Retrieves data points for a specific measurement type for focused analysis.
    /// </summary>
    /// <param name="measurementId">The measurement identifier to filter by</param>
    /// <returns>Filtered enumerable of data points for the specified measurement</returns>
    IEnumerable<DataPoint> GetDataByMeasurementId(string measurementId);
    
    /// <summary>
    /// Retrieves data points within a specific date range for time-bounded analysis.
    /// </summary>
    /// <param name="dateRange">The date range to filter data points</param>
    /// <returns>Filtered enumerable of data points within the specified date range</returns>
    IEnumerable<DataPoint> GetDataByDateRange(DateRange dateRange);

    // Analysis Support Methods
    /// <summary>
    /// Converts collected data points into structured time series format for correlation analysis.
    /// Detects temporal patterns and regularizes intervals for statistical analysis.
    /// </summary>
    /// <returns>Time series data with detected patterns and temporal structure</returns>
    TimeSeriesData GetTimeSeriesData();
    
    /// <summary>
    /// Calculates comprehensive statistical metrics for collected data including distributions,
    /// correlations, and data quality indicators.
    /// </summary>
    /// <returns>Statistical summary of collected measurements and market data</returns>
    DataStatistics GetStatistics();

    // Utility Methods
    /// <summary>
    /// Clears all collected data points and resets internal state for fresh data collection.
    /// Use with caution as this operation is irreversible.
    /// </summary>
    void Clear();
}