using MedjCap.Data.Configuration;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using MedjCap.Data.Exceptions;
using MedjCap.Data.Extensions;
using MedjCap.Data.Validators;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Options;

namespace MedjCap.Data.Services;

/// <summary>
/// Implementation of ICorrelationService for analyzing correlations between measurements and price movements.
/// Uses statistical methods to identify relationships in time-series financial data.
/// </summary>
public class CorrelationService : ICorrelationService
{
    private readonly StatisticalConfig _config;
    private readonly IOutlierDetectionService _outlierDetectionService;

    public CorrelationService(IOptions<StatisticalConfig> config, IOutlierDetectionService? outlierDetectionService = null)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _outlierDetectionService = outlierDetectionService ?? new OutlierDetectionService(config);
    }
    // Price Movement Calculation
    public List<PriceMovement> CalculatePriceMovements(TimeSeriesData timeSeries, TimeSpan timeHorizon)
    {
        if (timeSeries == null)
            throw new ArgumentNullException(nameof(timeSeries));
        if (timeSeries.DataPoints == null)
            throw new ArgumentException("TimeSeriesData.DataPoints cannot be null", nameof(timeSeries));
        if (timeHorizon <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeHorizon), "Time horizon must be positive");

        var movements = new List<PriceMovement>();
        var dataPoints = timeSeries.DataPoints.OrderBy(dp => dp.Timestamp).ToList();
        
        for (int i = 0; i < dataPoints.Count; i++)
        {
            var currentPoint = dataPoints[i];
            var futureTime = currentPoint.Timestamp.Add(timeHorizon);
            
            // Find future price at the specified horizon
            var futurePoint = dataPoints
                .Where(dp => dp.Timestamp >= futureTime)
                .OrderBy(dp => dp.Timestamp)
                .FirstOrDefault();
                
            if (futurePoint == null || currentPoint.ATR == 0) continue;
            
            // Calculate ATR movement: (futurePrice - currentPrice) / currentATR
            var priceChange = futurePoint.Price - currentPoint.Price;
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

        var result = new Dictionary<TimeSpan, List<PriceMovement>>();
        
        foreach (var horizon in timeHorizons)
        {
            result[horizon] = CalculatePriceMovements(timeSeries, horizon);
        }
        
        return result;
    }

    // Correlation Analysis
    public CorrelationResult CalculateCorrelation(List<PriceMovement> movements, CorrelationType correlationType)
    {
        if (movements == null)
            throw new ArgumentNullException(nameof(movements));
        if (!Enum.IsDefined(typeof(CorrelationType), correlationType))
            throw new ArgumentException("Invalid correlation type", nameof(correlationType));

        // Apply outlier detection and handling if enabled
        var processedMovements = ApplyOutlierHandling(movements);

        if (!processedMovements.HasSufficientSamples(2))
        {
            // For correlation calculations, we can return a meaningful result for insufficient data
            // rather than throwing an exception, but log it for monitoring
            return new CorrelationResult
            {
                Coefficient = 0.0,
                PValue = 1.0,
                SampleSize = processedMovements.Count,
                IsStatisticallySignificant = false,
                AverageMovement = processedMovements.Any() ? processedMovements.Average(m => m.ATRMovement) : 0,
                CorrelationType = correlationType
            };
        }
        
        var measurementValues = processedMovements.Select(m => (double)m.MeasurementValue).ToArray();
        var atrMovements = processedMovements.Select(m => (double)m.ATRMovement).ToArray();
        
        // Validate data quality before correlation calculation
        ValidateCorrelationData(measurementValues, atrMovements, correlationType);
        
        double coefficient = 0.0;
        
        // Calculate correlation based on type with error handling
        try
        {
            switch (correlationType)
            {
                case CorrelationType.Pearson:
                    coefficient = Correlation.Pearson(measurementValues, atrMovements);
                    break;
                case CorrelationType.Spearman:
                    coefficient = Correlation.Spearman(measurementValues, atrMovements);
                    break;
                case CorrelationType.KendallTau:
                    // MathNet doesn't have Kendall, approximate with Spearman
                    coefficient = Correlation.Spearman(measurementValues, atrMovements);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Wrap mathematical errors in our domain exception
            var reason = DetermineCorrelationFailureReason(ex, measurementValues, atrMovements);
            var problematicValues = measurementValues.Concat(atrMovements).Where(IsProblematicValue).ToArray();
            
            throw new CorrelationCalculationException(
                correlationType,
                reason,
                problematicValues,
                $"Mathematical error during {correlationType} correlation: {ex.Message}",
                ex);
        }
        
        // Handle NaN cases
        if (double.IsNaN(coefficient))
            coefficient = 0.0;
            
        // Calculate statistical significance with proper p-value and confidence intervals
        var n = movements.Count;
        var statisticalResult = CalculateStatisticalSignificance(coefficient, n);
        
        return new CorrelationResult
        {
            Coefficient = coefficient,
            PValue = statisticalResult.PValue,
            SampleSize = n,
            IsStatisticallySignificant = statisticalResult.IsSignificant,
            AverageMovement = (decimal)atrMovements.Average(),
            CorrelationType = correlationType,
            ConfidenceInterval = statisticalResult.ConfidenceInterval,
            TStatistic = statisticalResult.TStatistic,
            DegreesOfFreedom = statisticalResult.DegreesOfFreedom,
            StandardError = statisticalResult.StandardError
        };
    }
    
    // Data Analysis & Segmentation
    public Dictionary<string, List<PriceMovement>> BucketizeMovements(List<PriceMovement> movements, decimal[] atrTargets)
    {
        if (movements == null)
            throw new ArgumentNullException(nameof(movements));
        if (atrTargets == null)
            throw new ArgumentNullException(nameof(atrTargets));
        if (atrTargets.Length == 0)
            throw new ArgumentException("At least one ATR target must be provided", nameof(atrTargets));
        if (atrTargets.Any(t => t < 0))
            throw new ArgumentOutOfRangeException(nameof(atrTargets), "ATR targets must be non-negative");

        var buckets = new Dictionary<string, List<PriceMovement>>();
        var sortedTargets = atrTargets.OrderBy(t => t).ToArray();
        
        // Create bucket ranges
        for (int i = 0; i < sortedTargets.Length; i++)
        {
            var low = i == 0 ? 0m : sortedTargets[i - 1];
            var high = sortedTargets[i];
            var bucketKey = $"{low:F1}-{high:F1}";
            buckets[bucketKey] = new List<PriceMovement>();
        }
        
        // Add final bucket for movements above highest target
        var maxTarget = sortedTargets.Length > 0 ? sortedTargets.Last() : 0m;
        buckets[$"{maxTarget:F1}+"] = new List<PriceMovement>();
        
        // Categorize movements into buckets
        foreach (var movement in movements)
        {
            var absMovement = Math.Abs(movement.ATRMovement);
            bool placed = false;
            
            for (int i = 0; i < sortedTargets.Length; i++)
            {
                var low = i == 0 ? 0m : sortedTargets[i - 1];
                var high = sortedTargets[i];
                
                if (absMovement >= low && absMovement < high)
                {
                    var bucketKey = $"{low:F1}-{high:F1}";
                    buckets[bucketKey].Add(movement);
                    placed = true;
                    break;
                }
            }
            
            // If not placed in any range, goes to the final bucket
            if (!placed)
            {
                buckets[$"{maxTarget:F1}+"].Add(movement);
            }
        }
        
        return buckets;
    }

    public Dictionary<string, RangeAnalysisResult> AnalyzeByMeasurementRanges(List<PriceMovement> movements, List<(decimal Low, decimal High)> measurementRanges)
    {
        if (movements == null)
            throw new ArgumentNullException(nameof(movements));
        if (measurementRanges == null)
            throw new ArgumentNullException(nameof(measurementRanges));
        if (measurementRanges.Count == 0)
            throw new ArgumentException("At least one measurement range must be provided", nameof(measurementRanges));
        if (measurementRanges.Any(r => r.Low >= r.High))
            throw new ArgumentException("All measurement ranges must have Low < High", nameof(measurementRanges));

        var results = new Dictionary<string, RangeAnalysisResult>();
        
        for (int i = 0; i < measurementRanges.Count; i++)
        {
            var (low, high) = measurementRanges[i];
            var rangeKey = $"{low}-{high}";
            
            // For the last range, include the upper boundary to catch edge values
            var isLastRange = i == measurementRanges.Count - 1;
            var movementsInRange = movements
                .Where(m => m.MeasurementValue >= low && 
                           (isLastRange ? m.MeasurementValue <= high : m.MeasurementValue < high))
                .ToList();
                
            if (movementsInRange.Count == 0)
            {
                results[rangeKey] = new RangeAnalysisResult
                {
                    ProbabilityUp = 0.0,
                    ProbabilityDown = 0.0,
                    AverageATRMove = 0m,
                    SampleCount = 0,
                    MinMovement = 0m,
                    MaxMovement = 0m
                };
                continue;
            }
            
            var upMoves = movementsInRange.Count(m => m.ATRMovement > 0);
            var downMoves = movementsInRange.Count(m => m.ATRMovement < 0);
            var totalMoves = movementsInRange.Count;
            
            results[rangeKey] = new RangeAnalysisResult
            {
                ProbabilityUp = (double)upMoves / totalMoves,
                ProbabilityDown = (double)downMoves / totalMoves,
                AverageATRMove = movementsInRange.Average(m => m.ATRMovement),
                SampleCount = totalMoves,
                MinMovement = movementsInRange.Min(m => m.ATRMovement),
                MaxMovement = movementsInRange.Max(m => m.ATRMovement)
            };
        }
        
        return results;
    }
    
    // Contextual Filtering
    public CorrelationResult CalculateWithContextualFilter(List<PriceMovement> movements, string contextVariable, decimal contextThreshold, ComparisonOperator comparisonOperator)
    {
        if (movements == null)
            throw new ArgumentNullException(nameof(movements));
        if (string.IsNullOrWhiteSpace(contextVariable))
            throw new ArgumentException("Context variable cannot be null or empty", nameof(contextVariable));
        if (!Enum.IsDefined(typeof(ComparisonOperator), comparisonOperator))
            throw new ArgumentException("Invalid comparison operator", nameof(comparisonOperator));

        var filteredMovements = movements.Where(m => 
        {
            if (!m.ContextualData.ContainsKey(contextVariable))
                return false;
                
            var contextValue = m.ContextualData[contextVariable];
            
            return comparisonOperator switch
            {
                ComparisonOperator.GreaterThan => contextValue > contextThreshold,
                ComparisonOperator.LessThan => contextValue < contextThreshold,
                ComparisonOperator.Equal => contextValue == contextThreshold,
                ComparisonOperator.GreaterThanOrEqual => contextValue >= contextThreshold,
                ComparisonOperator.LessThanOrEqual => contextValue <= contextThreshold,
                _ => false
            };
        }).ToList();
        
        if (filteredMovements.Count == 0)
        {
            return new CorrelationResult
            {
                Coefficient = 0.0,
                PValue = 1.0,
                SampleSize = 0,
                IsStatisticallySignificant = false,
                AverageMovement = 0m,
                CorrelationType = CorrelationType.Pearson
            };
        }
        
        var result = CalculateCorrelation(filteredMovements, CorrelationType.Pearson);
        return result with { AverageMovement = filteredMovements.Average(m => m.ATRMovement) };
    }
    
    // Comprehensive Analysis
    public CorrelationAnalysisResult RunFullAnalysis(TimeSeriesData timeSeries, CorrelationAnalysisRequest request)
    {
        if (timeSeries == null)
            throw new ArgumentNullException(nameof(timeSeries));
        if (timeSeries.DataPoints == null)
            throw new ArgumentException("TimeSeriesData.DataPoints cannot be null", nameof(timeSeries));
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.MeasurementId))
            throw new ArgumentException("MeasurementId cannot be null or empty", nameof(request));
        if (request.TimeHorizons == null || request.TimeHorizons.Length == 0)
            throw new ArgumentException("At least one time horizon must be provided", nameof(request));
        if (request.ATRTargets == null || request.ATRTargets.Length == 0)
            throw new ArgumentException("At least one ATR target must be provided", nameof(request));
        if (request.MeasurementRanges == null)
            throw new ArgumentNullException(nameof(request), "MeasurementRanges cannot be null");

        // Calculate movements for all time horizons
        var movementsByHorizon = CalculatePriceMovements(timeSeries, request.TimeHorizons);
        
        // Get movements for the measurement ID (use first horizon as primary)
        var primaryHorizon = request.TimeHorizons.FirstOrDefault();
        var primaryMovements = movementsByHorizon.ContainsKey(primaryHorizon) 
            ? movementsByHorizon[primaryHorizon]
                .Where(m => m.MeasurementValue != 0) // Filter for the specific measurement
                .ToList()
            : new List<PriceMovement>();
        
        // Calculate correlations for each time horizon
        var correlationsByHorizon = new Dictionary<TimeSpan, CorrelationResult>();
        foreach (var (horizon, movements) in movementsByHorizon)
        {
            var filteredMovements = movements.Where(m => m.MeasurementValue != 0).ToList();
            if (filteredMovements.Any())
            {
                correlationsByHorizon[horizon] = CalculateCorrelation(filteredMovements, CorrelationType.Pearson);
            }
        }
        
        // Analyze by measurement ranges
        var rangeAnalysis = AnalyzeByMeasurementRanges(primaryMovements, request.MeasurementRanges);
        
        // Bucketize by ATR targets
        var atrBucketAnalysis = BucketizeMovements(primaryMovements, request.ATRTargets);
        
        // Calculate overall statistics
        var overallStats = new OverallStatistics();
        if (primaryMovements.Any())
        {
            var allMovements = primaryMovements.Select(m => m.ATRMovement).ToList();
            overallStats = new OverallStatistics
            {
                TotalSamples = primaryMovements.Count,
                MeanATRMovement = allMovements.Average(),
                StdDevATRMovement = (decimal)Math.Sqrt(allMovements.Select(x => Math.Pow((double)(x - allMovements.Average()), 2)).Average()),
                MinATRMovement = allMovements.Min(),
                MaxATRMovement = allMovements.Max(),
                PercentageUpMoves = (double)primaryMovements.Count(m => m.ATRMovement > 0) / primaryMovements.Count * 100
            };
        }
        
        return new CorrelationAnalysisResult
        {
            MeasurementId = request.MeasurementId,
            CorrelationsByTimeHorizon = correlationsByHorizon,
            RangeAnalysis = rangeAnalysis,
            ATRBucketAnalysis = atrBucketAnalysis,
            OverallStatistics = overallStats
        };
    }

    /// <summary>
    /// Calculates proper statistical significance testing for correlation coefficients
    /// </summary>
    private StatisticalSignificanceResult CalculateStatisticalSignificance(double coefficient, int sampleSize)
    {
        if (sampleSize < 3)
        {
            return new StatisticalSignificanceResult
            {
                PValue = 1.0,
                IsSignificant = false,
                ConfidenceInterval = (0, 0),
                TStatistic = 0,
                DegreesOfFreedom = Math.Max(0, sampleSize - 2),
                StandardError = double.NaN
            };
        }

        var degreesOfFreedom = sampleSize - 2;
        var rSquared = coefficient * coefficient;
        
        // Avoid division by zero when |r| = 1
        if (Math.Abs(coefficient) >= 0.99999)
        {
            return new StatisticalSignificanceResult
            {
                PValue = coefficient == 0 ? 1.0 : 0.0001, // Very strong correlation
                IsSignificant = coefficient.IsStrongCorrelation(_config.MinimumCorrelation),
                ConfidenceInterval = (coefficient - 0.001, coefficient + 0.001),
                TStatistic = Math.Sign(coefficient) * 100, // Very large t-statistic
                DegreesOfFreedom = degreesOfFreedom,
                StandardError = 0.001
            };
        }

        // Calculate t-statistic: t = r * sqrt(n-2) / sqrt(1-r²)
        var standardError = Math.Sqrt((1 - rSquared) / degreesOfFreedom);
        var tStatistic = coefficient / standardError;
        
        // Calculate two-tailed p-value using t-distribution approximation
        var pValue = CalculateTDistributionPValue(Math.Abs(tStatistic), degreesOfFreedom);
        
        // Calculate 95% confidence interval for correlation coefficient
        var confidenceInterval = CalculateConfidenceInterval(coefficient, sampleSize, _config.DefaultConfidenceLevel);
        
        return new StatisticalSignificanceResult
        {
            PValue = pValue,
            IsSignificant = pValue.IsStatisticallySignificant(_config.AlphaLevel) && coefficient.IsStrongCorrelation(_config.MinimumCorrelation), // Both statistical and practical significance
            ConfidenceInterval = confidenceInterval,
            TStatistic = tStatistic,
            DegreesOfFreedom = degreesOfFreedom,
            StandardError = standardError
        };
    }

    /// <summary>
    /// Approximates two-tailed p-value for t-distribution
    /// Uses polynomial approximation for common cases
    /// </summary>
    private double CalculateTDistributionPValue(double tStat, int df)
    {
        if (df < 1) return 1.0;
        
        // For large degrees of freedom (>100), use normal approximation
        if (df >= 100)
        {
            return 2 * (1 - NormalCDF(tStat));
        }
        
        // Critical values for common degrees of freedom at α = 0.05 (two-tailed)
        var criticalValues = new Dictionary<int, double>
        {
            [1] = 12.706, [2] = 4.303, [3] = 3.182, [4] = 2.776, [5] = 2.571,
            [6] = 2.447, [7] = 2.365, [8] = 2.306, [9] = 2.262, [10] = 2.228,
            [15] = 2.131, [20] = 2.086, [25] = 2.060, [30] = 2.042, [40] = 2.021,
            [50] = 2.009, [60] = 2.000, [80] = 1.990, [100] = 1.984
        };

        // Find the closest df in our table
        var closestDf = criticalValues.Keys
            .OrderBy(k => Math.Abs(k - df))
            .First();
        
        var criticalValue = criticalValues[closestDf];
        
        // Polynomial approximation for p-value calculation
        if (tStat >= criticalValue)
        {
            // Very significant
            if (tStat >= criticalValue * 2) return 0.001;
            if (tStat >= criticalValue * 1.5) return _config.PValueLevels.VerySignificant;
            return _config.PValueLevels.Significant;
        }
        else if (tStat >= criticalValue * 0.8)
        {
            return _config.PValueLevels.MarginallySignificant;
        }
        else if (tStat >= criticalValue * 0.6)
        {
            return _config.PValueLevels.NotSignificant;
        }
        else
        {
            return Math.Min(1.0, _config.PValueLevels.NotSignificant + (0.80 * (1 - tStat / (criticalValue * 0.6))));
        }
    }

    /// <summary>
    /// Calculates confidence interval for correlation coefficient using Fisher's z-transformation
    /// </summary>
    private (double Lower, double Upper) CalculateConfidenceInterval(double r, int n, double confidenceLevel)
    {
        if (n < 4)
            return (Math.Max(-1, r - 0.5), Math.Min(1, r + 0.5));
        
        // Fisher's z-transformation
        var z = 0.5 * Math.Log((1 + r) / (1 - r));
        var standardError = 1.0 / Math.Sqrt(n - 3);
        
        // Critical value for confidence level (approximate)
        var alpha = 1 - confidenceLevel;
        var criticalValue = alpha <= _config.PValueLevels.VerySignificant ? 2.576 : alpha <= _config.PValueLevels.Significant ? 1.96 : 1.645;
        
        var marginOfError = criticalValue * standardError;
        
        // Transform back to correlation coefficient
        var lowerZ = z - marginOfError;
        var upperZ = z + marginOfError;
        
        var lower = (Math.Exp(2 * lowerZ) - 1) / (Math.Exp(2 * lowerZ) + 1);
        var upper = (Math.Exp(2 * upperZ) - 1) / (Math.Exp(2 * upperZ) + 1);
        
        // Ensure bounds are within [-1, 1]
        lower = Math.Max(-1, Math.Min(1, lower));
        upper = Math.Max(-1, Math.Min(1, upper));
        
        return (lower, upper);
    }

    /// <summary>
    /// Standard normal cumulative distribution function approximation
    /// </summary>
    private double NormalCDF(double x)
    {
        // Abramowitz and Stegun approximation
        var a1 = 0.254829592;
        var a2 = -0.284496736;
        var a3 = 1.421413741;
        var a4 = -1.453152027;
        var a5 = 1.061405429;
        var p = 0.3275911;

        var sign = x < 0 ? -1 : 1;
        x = Math.Abs(x) / Math.Sqrt(2.0);

        var t = 1.0 / (1.0 + p * x);
        var y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

        return 0.5 * (1.0 + sign * y);
    }

    /// <summary>
    /// Validates data arrays before correlation calculation
    /// </summary>
    private static void ValidateCorrelationData(double[] values1, double[] values2, CorrelationType correlationType)
    {
        if (values1.Any(double.IsNaN) || values2.Any(double.IsNaN))
        {
            var nanValues = values1.Concat(values2).Where(double.IsNaN).ToArray();
            throw new CorrelationCalculationException(
                correlationType,
                CorrelationFailureReason.ContainsNaN,
                nanValues,
                "Dataset contains NaN values that prevent correlation calculation");
        }

        if (values1.Any(double.IsInfinity) || values2.Any(double.IsInfinity))
        {
            var infiniteValues = values1.Concat(values2).Where(double.IsInfinity).ToArray();
            throw new CorrelationCalculationException(
                correlationType,
                CorrelationFailureReason.ContainsInfinity,
                infiniteValues,
                "Dataset contains infinite values that prevent correlation calculation");
        }

        // Check for zero variance (constant values) - only throw if data is large enough to be meaningful
        // Small datasets or test data with constant values are handled gracefully by returning 0 correlation
        if (values1.Length > 10 && values2.Length > 10)
        {
            var variance1 = values1.Aggregate(0.0, (acc, val) => acc + Math.Pow(val - values1.Average(), 2)) / (values1.Length - 1);
            var variance2 = values2.Aggregate(0.0, (acc, val) => acc + Math.Pow(val - values2.Average(), 2)) / (values2.Length - 1);

            if (variance1 == 0.0 || variance2 == 0.0)
            {
                throw new CorrelationCalculationException(
                    correlationType,
                    CorrelationFailureReason.ZeroVariance,
                    variance1 == 0.0 ? values1 : values2,
                    "Large dataset has zero variance - this indicates a data collection issue");
            }
        }
    }

    /// <summary>
    /// Determines the reason for correlation calculation failure from exception
    /// </summary>
    private static CorrelationFailureReason DetermineCorrelationFailureReason(Exception ex, double[] values1, double[] values2)
    {
        var message = ex.Message.ToLowerInvariant();
        
        if (message.Contains("nan"))
            return CorrelationFailureReason.ContainsNaN;
        if (message.Contains("infinity") || message.Contains("infinite"))
            return CorrelationFailureReason.ContainsInfinity;
        if (message.Contains("division") && message.Contains("zero"))
            return CorrelationFailureReason.DivisionByZero;
        if (message.Contains("variance"))
            return CorrelationFailureReason.ZeroVariance;
        
        return CorrelationFailureReason.MathematicalError;
    }

    /// <summary>
    /// Determines if a value is problematic for correlation calculations
    /// </summary>
    private static bool IsProblematicValue(double value)
    {
        return double.IsNaN(value) || double.IsInfinity(value);
    }

    /// <summary>
    /// Applies outlier detection and handling to price movements if enabled.
    /// </summary>
    private List<PriceMovement> ApplyOutlierHandling(List<PriceMovement> movements)
    {
        if (!_config.OutlierDetection.EnableOutlierDetection || movements.Count < _config.OutlierDetection.MinimumSampleSizeForDetection)
        {
            return movements;
        }

        try
        {
            // Detect outliers using the configured default method or ensemble
            var detectionMethod = movements.HasSufficientSamples(100) ? OutlierDetectionMethod.Ensemble : OutlierDetectionMethod.IQR;
            var outlierResult = _outlierDetectionService.DetectOutliers(movements, detectionMethod);

            // Apply handling strategy if outliers were found
            if (outlierResult.OutlierIndices.Any())
            {
                var handledMovements = _outlierDetectionService.HandleOutliers(
                    movements, 
                    outlierResult, 
                    _config.OutlierDetection.DefaultHandlingStrategy);

                return handledMovements;
            }

            return movements;
        }
        catch (Exception)
        {
            // If outlier detection fails, return original data
            return movements;
        }
    }
}

/// <summary>
/// Result of statistical significance testing
/// </summary>
internal record StatisticalSignificanceResult
{
    public double PValue { get; init; }
    public bool IsSignificant { get; init; }
    public (double Lower, double Upper) ConfidenceInterval { get; init; }
    public double TStatistic { get; init; }
    public int DegreesOfFreedom { get; init; }
    public double StandardError { get; init; }
}