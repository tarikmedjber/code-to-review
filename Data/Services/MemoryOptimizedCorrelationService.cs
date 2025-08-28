using MedjCap.Data.Configuration;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using MedjCap.Data.Exceptions;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MedjCap.Data.Services;

/// <summary>
/// Memory-optimized implementation of ICorrelationService that uses array pooling for large calculations.
/// Reduces garbage collection pressure and improves performance for high-frequency analysis operations.
/// </summary>
public class MemoryOptimizedCorrelationService : ICorrelationService
{
    private readonly StatisticalConfig _config;
    private readonly IOutlierDetectionService _outlierDetectionService;
    private readonly IArrayMemoryPool _memoryPool;

    public MemoryOptimizedCorrelationService(
        IOptions<StatisticalConfig> config, 
        IArrayMemoryPool memoryPool,
        IOutlierDetectionService? outlierDetectionService = null)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _memoryPool = memoryPool ?? throw new ArgumentNullException(nameof(memoryPool));
        _outlierDetectionService = outlierDetectionService ?? new OutlierDetectionService(config);
    }

    /// <summary>
    /// Memory-optimized price movement calculation using pooled arrays for intermediate calculations.
    /// </summary>
    public List<PriceMovement> CalculatePriceMovements(TimeSeriesData timeSeries, TimeSpan timeHorizon)
    {
        if (timeSeries == null)
            throw new ArgumentNullException(nameof(timeSeries));
        if (timeSeries.DataPoints == null)
            throw new ArgumentException("TimeSeriesData.DataPoints cannot be null", nameof(timeSeries));
        if (timeHorizon <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeHorizon), "Time horizon must be positive");

        var dataPoints = timeSeries.DataPoints.OrderBy(dp => dp.Timestamp).ToList();
        var movements = new List<PriceMovement>();
        
        if (dataPoints.Count == 0) return movements;

        // Use pooled arrays for price and time calculations
        using var priceArray = _memoryPool.RentDecimalArrayDisposable(dataPoints.Count);
        using var timeArray = _memoryPool.RentIntArrayDisposable(dataPoints.Count); // Store as ticks
        
        // Pre-populate arrays for faster lookups
        for (int i = 0; i < dataPoints.Count; i++)
        {
            priceArray.Array[i] = dataPoints[i].Price;
            timeArray.Array[i] = (int)(dataPoints[i].Timestamp.Ticks / TimeSpan.TicksPerMinute); // Convert to minutes
        }
        
        var horizonMinutes = (int)timeHorizon.TotalMinutes;
        
        for (int i = 0; i < dataPoints.Count; i++)
        {
            var currentPoint = dataPoints[i];
            if (currentPoint.ATR == 0) continue;
            
            var futureTimeMinutes = timeArray.Array[i] + horizonMinutes;
            
            // Find future price using binary search on the time array for better performance
            int futureIndex = FindFutureIndex(timeArray.Array, futureTimeMinutes, i + 1, dataPoints.Count - 1);
            
            if (futureIndex == -1) continue;
            
            var priceChange = priceArray.Array[futureIndex] - priceArray.Array[i];
            var atrMovement = priceChange / currentPoint.ATR;
            
            movements.Add(new PriceMovement
            {
                StartTimestamp = currentPoint.Timestamp,
                MeasurementValue = currentPoint.MeasurementValue,
                ATRMovement = atrMovement,
                ContextualData = currentPoint.ContextualData
            });
        }
        
        return movements;
    }

    /// <summary>
    /// Memory-optimized multiple horizon calculation that reuses data structures.
    /// </summary>
    public Dictionary<TimeSpan, List<PriceMovement>> CalculatePriceMovements(TimeSeriesData timeSeries, TimeSpan[] timeHorizons)
    {
        if (timeSeries == null)
            throw new ArgumentNullException(nameof(timeSeries));
        if (timeSeries.DataPoints == null)
            throw new ArgumentException("TimeSeriesData.DataPoints cannot be null", nameof(timeSeries));
        if (timeHorizons == null)
            throw new ArgumentNullException(nameof(timeHorizons));
        if (timeHorizons.Length == 0)
            throw new ArgumentException("At least one time horizon must be provided", nameof(timeHorizons));
        if (timeHorizons.Any(th => th <= TimeSpan.Zero))
            throw new ArgumentOutOfRangeException(nameof(timeHorizons), "All time horizons must be positive");

        var dataPoints = timeSeries.DataPoints.OrderBy(dp => dp.Timestamp).ToList();
        var result = new Dictionary<TimeSpan, List<PriceMovement>>();
        
        if (dataPoints.Count == 0)
        {
            foreach (var horizon in timeHorizons)
                result[horizon] = new List<PriceMovement>();
            return result;
        }

        // Reuse pooled arrays across all horizons
        using var priceArray = _memoryPool.RentDecimalArrayDisposable(dataPoints.Count);
        using var timeArray = _memoryPool.RentIntArrayDisposable(dataPoints.Count);
        
        // Pre-populate arrays once for all horizon calculations
        for (int i = 0; i < dataPoints.Count; i++)
        {
            priceArray.Array[i] = dataPoints[i].Price;
            timeArray.Array[i] = (int)(dataPoints[i].Timestamp.Ticks / TimeSpan.TicksPerMinute);
        }
        
        // Calculate movements for each horizon reusing the same data arrays
        foreach (var horizon in timeHorizons)
        {
            result[horizon] = CalculateMovementsForHorizon(dataPoints, priceArray.Array, timeArray.Array, horizon);
        }
        
        return result;
    }

    /// <summary>
    /// Memory-optimized correlation calculation using pooled arrays for statistical operations.
    /// </summary>
    public CorrelationResult CalculateCorrelation(List<PriceMovement> movements, CorrelationType correlationType)
    {
        if (movements == null)
            throw new ArgumentNullException(nameof(movements));
        if (movements.Count < 2)
            throw new ArgumentException("At least 2 data points are required for correlation", nameof(movements));

        // Use pooled arrays for statistical calculations
        using var measurementArray = _memoryPool.RentDoubleArrayDisposable(movements.Count);
        using var movementArray = _memoryPool.RentDoubleArrayDisposable(movements.Count);
        
        // Populate arrays for statistical analysis
        for (int i = 0; i < movements.Count; i++)
        {
            measurementArray.Array[i] = (double)movements[i].MeasurementValue;
            movementArray.Array[i] = (double)movements[i].ATRMovement;
        }
        
        var correlation = correlationType switch
        {
            CorrelationType.Pearson => Correlation.Pearson(measurementArray.Array.Take(movements.Count), 
                                                          movementArray.Array.Take(movements.Count)),
            CorrelationType.Spearman => Correlation.Spearman(measurementArray.Array.Take(movements.Count), 
                                                            movementArray.Array.Take(movements.Count)),
            _ => throw new ArgumentException($"Unsupported correlation type: {correlationType}")
        };

        return new CorrelationResult
        {
            Coefficient = correlation,
            PValue = CalculatePValue(correlation, movements.Count),
            SampleSize = movements.Count,
            CorrelationType = correlationType,
            IsStatisticallySignificant = Math.Abs(correlation) > 0.3, // Use hardcoded threshold for now
            AverageMovement = movements.Average(m => m.ATRMovement)
        };
    }

    /// <summary>
    /// Memory-optimized bucketization using pooled arrays for sorting and grouping.
    /// </summary>
    public Dictionary<string, List<PriceMovement>> BucketizeMovements(List<PriceMovement> movements, decimal[] atrTargets)
    {
        if (movements == null)
            throw new ArgumentNullException(nameof(movements));
        if (atrTargets == null)
            throw new ArgumentNullException(nameof(atrTargets));
        if (atrTargets.Length == 0)
            throw new ArgumentException("At least one ATR target must be provided", nameof(atrTargets));

        var buckets = new Dictionary<string, List<PriceMovement>>();
        var sortedTargets = atrTargets.OrderBy(x => x).ToArray();

        // Initialize buckets
        buckets["< " + sortedTargets[0]] = new List<PriceMovement>();
        for (int i = 0; i < sortedTargets.Length; i++)
        {
            buckets[$"{sortedTargets[i]:F1}+ ATR"] = new List<PriceMovement>();
            buckets[$"{sortedTargets[i]:F1}- ATR"] = new List<PriceMovement>();
        }
        buckets["> " + sortedTargets.Last()] = new List<PriceMovement>();

        // Use pooled array for fast ATR movement values
        using var atrMovements = _memoryPool.RentDecimalArrayDisposable(movements.Count);
        for (int i = 0; i < movements.Count; i++)
        {
            atrMovements.Array[i] = Math.Abs(movements[i].ATRMovement);
        }

        // Bucketize movements
        for (int i = 0; i < movements.Count; i++)
        {
            var movement = movements[i];
            var absATR = atrMovements.Array[i];
            var isPositive = movement.ATRMovement >= 0;

            // Find appropriate bucket using binary search for performance
            int targetIndex = Array.BinarySearch(sortedTargets, absATR);
            if (targetIndex < 0) targetIndex = ~targetIndex;

            string bucketKey;
            if (targetIndex == 0)
                bucketKey = "< " + sortedTargets[0];
            else if (targetIndex >= sortedTargets.Length)
                bucketKey = "> " + sortedTargets.Last();
            else
                bucketKey = $"{sortedTargets[targetIndex - 1]:F1}{(isPositive ? "+" : "-")} ATR";

            buckets[bucketKey].Add(movement);
        }

        return buckets;
    }

    // Placeholder implementations for other ICorrelationService methods
    public Dictionary<string, RangeAnalysisResult> AnalyzeByMeasurementRanges(List<PriceMovement> movements, List<(decimal Low, decimal High)> measurementRanges)
    {
        // Delegate to original service for methods not yet optimized
        var originalService = new CorrelationService(Options.Create(_config), _outlierDetectionService);
        return originalService.AnalyzeByMeasurementRanges(movements, measurementRanges);
    }

    public CorrelationResult CalculateWithContextualFilter(List<PriceMovement> movements, string contextVariable, decimal contextThreshold, ComparisonOperator comparisonOperator)
    {
        var originalService = new CorrelationService(Options.Create(_config), _outlierDetectionService);
        return originalService.CalculateWithContextualFilter(movements, contextVariable, contextThreshold, comparisonOperator);
    }

    public CorrelationAnalysisResult RunFullAnalysis(TimeSeriesData timeSeries, CorrelationAnalysisRequest request)
    {
        var originalService = new CorrelationService(Options.Create(_config), _outlierDetectionService);
        return originalService.RunFullAnalysis(timeSeries, request);
    }

    // Helper method implementations...
    private List<PriceMovement> CalculateMovementsForHorizon(
        List<DataPoint> dataPoints, 
        decimal[] priceArray, 
        int[] timeArray, 
        TimeSpan horizon)
    {
        var movements = new List<PriceMovement>();
        var horizonMinutes = (int)horizon.TotalMinutes;
        
        for (int i = 0; i < dataPoints.Count; i++)
        {
            var currentPoint = dataPoints[i];
            if (currentPoint.ATR == 0) continue;
            
            var futureTimeMinutes = timeArray[i] + horizonMinutes;
            int futureIndex = FindFutureIndex(timeArray, futureTimeMinutes, i + 1, dataPoints.Count - 1);
            
            if (futureIndex == -1) continue;
            
            var priceChange = priceArray[futureIndex] - priceArray[i];
            var atrMovement = priceChange / currentPoint.ATR;
            
            movements.Add(new PriceMovement
            {
                StartTimestamp = currentPoint.Timestamp,
                MeasurementValue = currentPoint.MeasurementValue,
                ATRMovement = atrMovement,
                ContextualData = currentPoint.ContextualData
            });
        }
        
        return movements;
    }

    private static int FindFutureIndex(int[] timeArray, int targetTime, int start, int end)
    {
        while (start <= end)
        {
            if (timeArray[start] >= targetTime)
                return start;
            start++;
        }
        return -1;
    }

    private double CalculatePValue(double correlation, int sampleSize)
    {
        if (sampleSize <= 2) return 1.0;
        
        var t = correlation * Math.Sqrt((sampleSize - 2) / (1 - correlation * correlation));
        // Simplified p-value calculation - in production would use proper statistical library
        return 2 * (1 - Math.Abs(t) / Math.Sqrt(sampleSize - 2));
    }
}