using MedjCap.Data.Infrastructure.Configuration.Options;
using MedjCap.Data.Infrastructure.Interfaces;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.DataQuality.Interfaces;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Statistics.Models;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace MedjCap.Data.DataQuality.Services;

/// <summary>
/// Comprehensive outlier detection service implementing multiple algorithms for robust anomaly detection.
/// Supports IQR, Z-Score, Modified Z-Score, Isolation Forest, and ensemble methods.
/// </summary>
public class OutlierDetectionService : IOutlierDetectionService
{
    private readonly OutlierDetectionConfig _config;
    private readonly StatisticalConfig _statisticalConfig;

    public OutlierDetectionService(IOptions<StatisticalConfig> statisticalConfig)
    {
        _statisticalConfig = statisticalConfig?.Value ?? throw new ArgumentNullException(nameof(statisticalConfig));
        _config = _statisticalConfig.OutlierDetection;
    }

    /// <summary>
    /// Detects outliers using the specified method.
    /// </summary>
    public OutlierDetectionResult DetectOutliers(List<PriceMovement> data, OutlierDetectionMethod method)
    {
        if (data == null || !data.Any())
            return CreateEmptyResult(method);

        if (data.Count < _config.MinimumSampleSizeForDetection)
        {
            return CreateEmptyResult(method, $"Insufficient data for outlier detection (need {_config.MinimumSampleSizeForDetection}, got {data.Count})");
        }

        var stopwatch = Stopwatch.StartNew();
        var result = method switch
        {
            OutlierDetectionMethod.IQR => DetectOutliersIQR(data),
            OutlierDetectionMethod.ZScore => DetectOutliersZScore(data),
            OutlierDetectionMethod.ModifiedZScore => DetectOutliersModifiedZScore(data),
            OutlierDetectionMethod.IsolationForest => DetectOutliersIsolationForest(data),
            OutlierDetectionMethod.Ensemble => DetectOutliersEnsemble(data),
            _ => throw new ArgumentException($"Unsupported outlier detection method: {method}")
        };

        stopwatch.Stop();
        return result with { ExecutionTime = stopwatch.Elapsed };
    }

    /// <summary>
    /// Applies outlier handling strategy to the data.
    /// </summary>
    public List<PriceMovement> HandleOutliers(List<PriceMovement> data, OutlierDetectionResult detectionResult, OutlierHandlingStrategy strategy)
    {
        if (!detectionResult.OutlierIndices.Any())
            return data;

        var handledData = new List<PriceMovement>(data);
        var measurementValues = data.Select(m => m.MeasurementValue).ToList();
        var atrMovements = data.Select(m => m.ATRMovement).ToList();

        return strategy switch
        {
            OutlierHandlingStrategy.Remove => RemoveOutliers(handledData, detectionResult.OutlierIndices),
            OutlierHandlingStrategy.Cap => CapOutliers(handledData, measurementValues, atrMovements),
            OutlierHandlingStrategy.ReplaceWithMedian => ReplaceWithMedian(handledData, detectionResult.OutlierIndices, measurementValues, atrMovements),
            OutlierHandlingStrategy.LogTransform => ApplyLogTransform(handledData),
            OutlierHandlingStrategy.Flag => FlagOutliers(handledData, detectionResult.OutlierIndices),
            _ => handledData
        };
    }

    /// <summary>
    /// Comprehensive outlier analysis using multiple methods.
    /// </summary>
    public OutlierAnalysisResult AnalyzeOutliers(List<PriceMovement> data)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var methodResults = new Dictionary<OutlierDetectionMethod, OutlierDetectionResult>();

        // Run all detection methods
        var methods = new[] 
        { 
            OutlierDetectionMethod.IQR, 
            OutlierDetectionMethod.ZScore, 
            OutlierDetectionMethod.ModifiedZScore,
            OutlierDetectionMethod.IsolationForest
        };

        foreach (var method in methods)
        {
            try
            {
                methodResults[method] = DetectOutliers(data, method);
            }
            catch (Exception ex)
            {
                methodResults[method] = CreateEmptyResult(method, $"Detection failed: {ex.Message}");
            }
        }

        // Find consensus outliers
        var allOutliers = methodResults.Values.SelectMany(r => r.OutlierIndices).Distinct().ToList();
        var consensusOutliers = new List<int>();
        var outlierConfidence = new Dictionary<int, double>();

        foreach (var outlierIndex in allOutliers)
        {
            var agreementCount = methodResults.Values.Count(r => r.OutlierIndices.Contains(outlierIndex));
            var confidence = (double)agreementCount / methods.Length;
            outlierConfidence[outlierIndex] = confidence;

            if (confidence >= _config.EnsembleConsensusThreshold)
            {
                consensusOutliers.Add(outlierIndex);
            }
        }

        totalStopwatch.Stop();

        return new OutlierAnalysisResult
        {
            MethodResults = methodResults,
            ConsensusOutliers = consensusOutliers,
            OutlierConfidence = outlierConfidence,
            RecommendedStrategy = DetermineRecommendedStrategy(methodResults, consensusOutliers.Count, data.Count),
            ImpactAssessment = AssessOutlierImpact(data, consensusOutliers),
            TotalExecutionTime = totalStopwatch.Elapsed
        };
    }

    /// <summary>
    /// Assesses data quality including outlier analysis.
    /// </summary>
    public DataQualityReport AssessDataQuality(List<PriceMovement> data)
    {
        var issues = new List<DataQualityIssue>();
        var metrics = new Dictionary<string, double>();
        var recommendations = new List<string>();

        if (!data.Any())
        {
            return new DataQualityReport
            {
                QualityScore = 0,
                Issues = new List<DataQualityIssue> { new() { IssueType = "No Data", Description = "Dataset is empty", Severity = IssueSeverity.Critical } },
                Recommendations = new List<string> { "Provide valid data for analysis" }
            };
        }

        // Basic data validation
        var measurementValues = data.Select(m => (double)m.MeasurementValue).ToList();
        var atrMovements = data.Select(m => (double)m.ATRMovement).ToList();

        // Check for missing or invalid values
        var invalidMeasurements = data.Count(m => m.MeasurementValue == 0);
        var invalidATR = data.Count(m => m.ATRMovement == 0);

        if (invalidMeasurements > 0)
        {
            issues.Add(new DataQualityIssue
            {
                IssueType = "Invalid Measurements",
                Description = $"{invalidMeasurements} data points have zero measurement values",
                Severity = invalidMeasurements > data.Count * 0.1 ? IssueSeverity.Error : IssueSeverity.Warning,
                AffectedDataPoints = invalidMeasurements,
                ImpactScore = (double)invalidMeasurements / data.Count
            });
        }

        // Outlier analysis
        if (_config.EnableOutlierDetection && data.Count >= _config.MinimumSampleSizeForDetection)
        {
            var outlierAnalysis = AnalyzeOutliers(data);
            var outlierPercentage = (double)outlierAnalysis.ConsensusOutliers.Count / data.Count;

            if (outlierPercentage > _config.MaxOutlierPercentage)
            {
                issues.Add(new DataQualityIssue
                {
                    IssueType = "Excessive Outliers",
                    Description = $"{outlierPercentage:P1} of data points are outliers (threshold: {_config.MaxOutlierPercentage:P1})",
                    Severity = IssueSeverity.Warning,
                    AffectedDataPoints = outlierAnalysis.ConsensusOutliers.Count,
                    ImpactScore = Math.Min(1.0, outlierPercentage / _config.MaxOutlierPercentage)
                });

                recommendations.Add($"Consider applying outlier handling strategy: {outlierAnalysis.RecommendedStrategy}");
            }

            metrics["OutlierPercentage"] = outlierPercentage;
            metrics["OutlierImpactSeverity"] = (double)outlierAnalysis.ImpactAssessment.Severity;
        }

        // Statistical quality metrics
        var measurementStats = new DescriptiveStatistics(measurementValues);
        var atrStats = new DescriptiveStatistics(atrMovements);

        metrics["DataPoints"] = data.Count;
        metrics["MeasurementSkewness"] = measurementStats.Skewness;
        metrics["MeasurementKurtosis"] = measurementStats.Kurtosis;
        metrics["ATRSkewness"] = atrStats.Skewness;
        metrics["ATRKurtosis"] = atrStats.Kurtosis;

        // Check for distribution issues
        if (Math.Abs(measurementStats.Skewness) > 2)
        {
            issues.Add(new DataQualityIssue
            {
                IssueType = "Skewed Distribution",
                Description = $"Measurement values are highly skewed (skewness: {measurementStats.Skewness:F2})",
                Severity = IssueSeverity.Info,
                AffectedDataPoints = data.Count,
                ImpactScore = Math.Min(1.0, Math.Abs(measurementStats.Skewness) / 3)
            });
        }

        // Calculate overall quality score
        var qualityScore = CalculateQualityScore(issues, metrics, data.Count);

        return new DataQualityReport
        {
            QualityScore = qualityScore,
            Issues = issues,
            Recommendations = recommendations,
            QualityMetrics = metrics
        };
    }

    #region Private Methods - Outlier Detection Algorithms

    private OutlierDetectionResult DetectOutliersIQR(List<PriceMovement> data)
    {
        var measurementValues = data.Select(m => (double)m.MeasurementValue).OrderBy(x => x).ToList();
        var n = measurementValues.Count;
        
        var q1 = measurementValues[(int)(n * 0.25)];
        var q3 = measurementValues[(int)(n * 0.75)];
        var iqr = q3 - q1;
        
        var lowerBound = q1 - _config.IQRMultiplier * iqr;
        var upperBound = q3 + _config.IQRMultiplier * iqr;
        
        var outlierIndices = new List<int>();
        var outlierScores = new Dictionary<int, double>();
        
        for (int i = 0; i < data.Count; i++)
        {
            var value = (double)data[i].MeasurementValue;
            if (value < lowerBound || value > upperBound)
            {
                outlierIndices.Add(i);
                var distanceFromBound = Math.Min(Math.Abs(value - lowerBound), Math.Abs(value - upperBound));
                outlierScores[i] = distanceFromBound / iqr;
            }
        }

        return new OutlierDetectionResult
        {
            Method = OutlierDetectionMethod.IQR,
            OutlierIndices = outlierIndices,
            OutlierPercentage = (double)outlierIndices.Count / data.Count,
            DetectionThresholds = new Dictionary<string, double>
            {
                ["Q1"] = q1,
                ["Q3"] = q3,
                ["IQR"] = iqr,
                ["LowerBound"] = lowerBound,
                ["UpperBound"] = upperBound,
                ["Multiplier"] = _config.IQRMultiplier
            },
            OriginalStatistics = CalculateStatistics(data),
            CleanedStatistics = CalculateStatistics(data, outlierIndices),
            OutlierScores = outlierScores,
            Diagnostics = new List<string> { $"IQR method detected {outlierIndices.Count} outliers using {_config.IQRMultiplier}x multiplier" }
        };
    }

    private OutlierDetectionResult DetectOutliersZScore(List<PriceMovement> data)
    {
        var measurementValues = data.Select(m => (double)m.MeasurementValue).ToList();
        var stats = new DescriptiveStatistics(measurementValues);
        
        var outlierIndices = new List<int>();
        var outlierScores = new Dictionary<int, double>();
        
        for (int i = 0; i < data.Count; i++)
        {
            var zScore = Math.Abs(((double)data[i].MeasurementValue - stats.Mean) / stats.StandardDeviation);
            if (zScore > _config.ZScoreThreshold)
            {
                outlierIndices.Add(i);
                outlierScores[i] = zScore;
            }
        }

        return new OutlierDetectionResult
        {
            Method = OutlierDetectionMethod.ZScore,
            OutlierIndices = outlierIndices,
            OutlierPercentage = (double)outlierIndices.Count / data.Count,
            DetectionThresholds = new Dictionary<string, double>
            {
                ["Mean"] = stats.Mean,
                ["StandardDeviation"] = stats.StandardDeviation,
                ["ZScoreThreshold"] = _config.ZScoreThreshold
            },
            OriginalStatistics = CalculateStatistics(data),
            CleanedStatistics = CalculateStatistics(data, outlierIndices),
            OutlierScores = outlierScores,
            Diagnostics = new List<string> { $"Z-Score method detected {outlierIndices.Count} outliers using threshold {_config.ZScoreThreshold}" }
        };
    }

    private OutlierDetectionResult DetectOutliersModifiedZScore(List<PriceMovement> data)
    {
        var measurementValues = data.Select(m => (double)m.MeasurementValue).ToList();
        var median = measurementValues.Median();
        var deviations = measurementValues.Select(x => Math.Abs(x - median)).ToList();
        var mad = deviations.Median();
        
        var outlierIndices = new List<int>();
        var outlierScores = new Dictionary<int, double>();
        
        // Avoid division by zero
        if (mad < 1e-10)
        {
            return new OutlierDetectionResult
            {
                Method = OutlierDetectionMethod.ModifiedZScore,
                OutlierIndices = outlierIndices,
                OutlierPercentage = 0,
                Diagnostics = new List<string> { "Modified Z-Score: MAD too small, no outliers detected" }
            };
        }
        
        for (int i = 0; i < data.Count; i++)
        {
            var modifiedZScore = 0.6745 * Math.Abs((double)data[i].MeasurementValue - median) / mad;
            if (modifiedZScore > _config.ModifiedZScoreThreshold)
            {
                outlierIndices.Add(i);
                outlierScores[i] = modifiedZScore;
            }
        }

        return new OutlierDetectionResult
        {
            Method = OutlierDetectionMethod.ModifiedZScore,
            OutlierIndices = outlierIndices,
            OutlierPercentage = (double)outlierIndices.Count / data.Count,
            DetectionThresholds = new Dictionary<string, double>
            {
                ["Median"] = median,
                ["MAD"] = mad,
                ["ModifiedZScoreThreshold"] = _config.ModifiedZScoreThreshold
            },
            OriginalStatistics = CalculateStatistics(data),
            CleanedStatistics = CalculateStatistics(data, outlierIndices),
            OutlierScores = outlierScores,
            Diagnostics = new List<string> { $"Modified Z-Score method detected {outlierIndices.Count} outliers using MAD-based threshold {_config.ModifiedZScoreThreshold}" }
        };
    }

    private OutlierDetectionResult DetectOutliersIsolationForest(List<PriceMovement> data)
    {
        // For now, implement a simplified isolation-like algorithm using statistical methods
        // This can be enhanced later with proper machine learning libraries
        try
        {
            var measurementValues = data.Select(m => (double)m.MeasurementValue).ToList();
            var atrValues = data.Select(m => (double)m.ATRMovement).ToList();
            
            // Combine Z-score and IQR methods for pseudo-isolation effect
            var measurementStats = new DescriptiveStatistics(measurementValues);
            var atrStats = new DescriptiveStatistics(atrValues);
            
            var outlierIndices = new List<int>();
            var outlierScores = new Dictionary<int, double>();
            
            for (int i = 0; i < data.Count; i++)
            {
                var measurementZ = Math.Abs(((double)data[i].MeasurementValue - measurementStats.Mean) / measurementStats.StandardDeviation);
                var atrZ = Math.Abs(((double)data[i].ATRMovement - atrStats.Mean) / atrStats.StandardDeviation);
                
                // Combined isolation score (higher = more isolated)
                var isolationScore = (measurementZ + atrZ) / 2.0;
                
                if (isolationScore > 2.0) // Configurable threshold
                {
                    outlierIndices.Add(i);
                    outlierScores[i] = isolationScore;
                }
            }
            
            // Limit to contamination ratio
            if (outlierIndices.Count > data.Count * _config.IsolationForestContamination)
            {
                var topOutliers = outlierScores.OrderByDescending(kvp => kvp.Value)
                    .Take((int)(data.Count * _config.IsolationForestContamination))
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                outlierIndices = topOutliers;
                outlierScores = outlierScores.Where(kvp => topOutliers.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return new OutlierDetectionResult
            {
                Method = OutlierDetectionMethod.IsolationForest,
                OutlierIndices = outlierIndices,
                OutlierPercentage = (double)outlierIndices.Count / data.Count,
                DetectionThresholds = new Dictionary<string, double>
                {
                    ["IsolationThreshold"] = 2.0,
                    ["ContaminationRatio"] = _config.IsolationForestContamination
                },
                OriginalStatistics = CalculateStatistics(data),
                CleanedStatistics = CalculateStatistics(data, outlierIndices),
                OutlierScores = outlierScores,
                Diagnostics = new List<string> { $"Simplified Isolation method detected {outlierIndices.Count} outliers using contamination ratio {_config.IsolationForestContamination:P1}" }
            };
        }
        catch (Exception ex)
        {
            return new OutlierDetectionResult
            {
                Method = OutlierDetectionMethod.IsolationForest,
                OutlierIndices = new List<int>(),
                OutlierPercentage = 0,
                Diagnostics = new List<string> { $"Isolation method failed: {ex.Message}" }
            };
        }
    }

    private OutlierDetectionResult DetectOutliersEnsemble(List<PriceMovement> data)
    {
        var methods = new[] 
        { 
            OutlierDetectionMethod.IQR, 
            OutlierDetectionMethod.ZScore, 
            OutlierDetectionMethod.ModifiedZScore,
            OutlierDetectionMethod.IsolationForest
        };

        var methodResults = new Dictionary<OutlierDetectionMethod, OutlierDetectionResult>();
        foreach (var method in methods)
        {
            try
            {
                methodResults[method] = DetectOutliers(data, method);
            }
            catch
            {
                // Skip failed methods
            }
        }

        var allOutliers = methodResults.Values.SelectMany(r => r.OutlierIndices).Distinct().ToList();
        var consensusOutliers = new List<int>();
        var outlierScores = new Dictionary<int, double>();

        foreach (var outlierIndex in allOutliers)
        {
            var agreementCount = methodResults.Values.Count(r => r.OutlierIndices.Contains(outlierIndex));
            var confidence = (double)agreementCount / methodResults.Count;
            outlierScores[outlierIndex] = confidence;

            if (confidence >= _config.EnsembleConsensusThreshold)
            {
                consensusOutliers.Add(outlierIndex);
            }
        }

        return new OutlierDetectionResult
        {
            Method = OutlierDetectionMethod.Ensemble,
            OutlierIndices = consensusOutliers,
            OutlierPercentage = (double)consensusOutliers.Count / data.Count,
            DetectionThresholds = new Dictionary<string, double>
            {
                ["ConsensusThreshold"] = _config.EnsembleConsensusThreshold,
                ["MethodsUsed"] = methodResults.Count
            },
            OriginalStatistics = CalculateStatistics(data),
            CleanedStatistics = CalculateStatistics(data, consensusOutliers),
            OutlierScores = outlierScores,
            Diagnostics = new List<string> { $"Ensemble method detected {consensusOutliers.Count} consensus outliers from {methodResults.Count} methods" }
        };
    }

    #endregion

    #region Private Methods - Outlier Handling

    private List<PriceMovement> RemoveOutliers(List<PriceMovement> data, List<int> outlierIndices)
    {
        var result = new List<PriceMovement>();
        for (int i = 0; i < data.Count; i++)
        {
            if (!outlierIndices.Contains(i))
            {
                result.Add(data[i]);
            }
        }
        return result;
    }

    private List<PriceMovement> CapOutliers(List<PriceMovement> data, List<decimal> measurementValues, List<decimal> atrMovements)
    {
        var measurementPercentiles = CalculatePercentiles(measurementValues.Select(v => (double)v).ToList());
        var atrPercentiles = CalculatePercentiles(atrMovements.Select(v => (double)v).ToList());
        
        var result = new List<PriceMovement>();
        for (int i = 0; i < data.Count; i++)
        {
            var cappedMeasurement = Math.Max(measurementPercentiles.Lower, Math.Min(measurementPercentiles.Upper, (double)data[i].MeasurementValue));
            var cappedATR = Math.Max(atrPercentiles.Lower, Math.Min(atrPercentiles.Upper, (double)data[i].ATRMovement));
            
            result.Add(data[i] with 
            { 
                MeasurementValue = (decimal)cappedMeasurement,
                ATRMovement = (decimal)cappedATR
            });
        }
        return result;
    }

    private List<PriceMovement> ReplaceWithMedian(List<PriceMovement> data, List<int> outlierIndices, List<decimal> measurementValues, List<decimal> atrMovements)
    {
        var measurementMedian = measurementValues.Select(v => (double)v).Median();
        var atrMedian = atrMovements.Select(v => (double)v).Median();
        
        var result = new List<PriceMovement>();
        for (int i = 0; i < data.Count; i++)
        {
            if (outlierIndices.Contains(i))
            {
                result.Add(data[i] with 
                { 
                    MeasurementValue = (decimal)measurementMedian,
                    ATRMovement = (decimal)atrMedian
                });
            }
            else
            {
                result.Add(data[i]);
            }
        }
        return result;
    }

    private List<PriceMovement> ApplyLogTransform(List<PriceMovement> data)
    {
        // Apply log(1 + x) transformation to reduce impact of extreme values
        var result = new List<PriceMovement>();
        foreach (var movement in data)
        {
            var logMeasurement = Math.Log(1 + Math.Max(0, (double)movement.MeasurementValue));
            var logATR = Math.Sign((double)movement.ATRMovement) * Math.Log(1 + Math.Abs((double)movement.ATRMovement));
            
            result.Add(movement with 
            { 
                MeasurementValue = (decimal)logMeasurement,
                ATRMovement = (decimal)logATR
            });
        }
        return result;
    }

    private List<PriceMovement> FlagOutliers(List<PriceMovement> data, List<int> outlierIndices)
    {
        // Add outlier flags to contextual data
        var result = new List<PriceMovement>();
        for (int i = 0; i < data.Count; i++)
        {
            var contextualData = new Dictionary<string, decimal>(data[i].ContextualData);
            contextualData["IsOutlier"] = outlierIndices.Contains(i) ? 1m : 0m;
            
            result.Add(data[i] with { ContextualData = contextualData });
        }
        return result;
    }

    #endregion

    #region Private Helper Methods

    private OutlierDetectionResult CreateEmptyResult(OutlierDetectionMethod method, string? diagnostic = null)
    {
        var diagnostics = new List<string>();
        if (!string.IsNullOrEmpty(diagnostic))
            diagnostics.Add(diagnostic);
            
        return new OutlierDetectionResult
        {
            Method = method,
            OutlierIndices = new List<int>(),
            OutlierPercentage = 0,
            Diagnostics = diagnostics
        };
    }

    private DataStatistics CalculateStatistics(List<PriceMovement> data, List<int>? excludeIndices = null)
    {
        var filteredData = excludeIndices != null 
            ? data.Where((item, index) => !excludeIndices.Contains(index)).ToList()
            : data;

        if (!filteredData.Any())
        {
            return new DataStatistics();
        }

        var measurementValues = filteredData.Select(m => (double)m.MeasurementValue).ToList();
        var atrValues = filteredData.Select(m => (double)m.ATRMovement).ToList();
        
        var measurementStats = new DescriptiveStatistics(measurementValues);
        var atrStats = new DescriptiveStatistics(atrValues);

        return new DataStatistics
        {
            SampleCount = filteredData.Count,
            MeasurementMean = (decimal)measurementStats.Mean,
            MeasurementStdDev = (decimal)measurementStats.StandardDeviation,
            MeasurementMin = (decimal)measurementStats.Minimum,
            MeasurementMax = (decimal)measurementStats.Maximum,
            ATRMean = (decimal)atrStats.Mean,
            ATRStdDev = (decimal)atrStats.StandardDeviation,
            ATRMin = (decimal)atrStats.Minimum,
            ATRMax = (decimal)atrStats.Maximum
        };
    }

    private OutlierImpactAssessment AssessOutlierImpact(List<PriceMovement> data, List<int> outlierIndices)
    {
        if (!outlierIndices.Any())
        {
            return new OutlierImpactAssessment { Severity = ImpactSeverity.Low };
        }

        var originalStats = CalculateStatistics(data);
        var cleanedStats = CalculateStatistics(data, outlierIndices);

        var meanImpact = Math.Abs((double)(cleanedStats.MeasurementMean - originalStats.MeasurementMean)) / (double)originalStats.MeasurementMean;
        var stdImpact = Math.Abs((double)(cleanedStats.MeasurementStdDev - originalStats.MeasurementStdDev)) / (double)originalStats.MeasurementStdDev;

        var maxImpact = Math.Max(meanImpact, stdImpact);
        var severity = maxImpact switch
        {
            > 0.2 => ImpactSeverity.Critical,
            > 0.1 => ImpactSeverity.High,
            > 0.05 => ImpactSeverity.Medium,
            _ => ImpactSeverity.Low
        };

        return new OutlierImpactAssessment
        {
            MeanImpact = meanImpact,
            StandardDeviationImpact = stdImpact,
            Severity = severity
        };
    }

    private OutlierHandlingStrategy DetermineRecommendedStrategy(Dictionary<OutlierDetectionMethod, OutlierDetectionResult> results, int consensusOutliers, int totalDataPoints)
    {
        var outlierPercentage = (double)consensusOutliers / totalDataPoints;
        
        if (outlierPercentage > 0.15)
            return OutlierHandlingStrategy.LogTransform;
        else if (outlierPercentage > 0.1)
            return OutlierHandlingStrategy.Cap;
        else if (outlierPercentage > 0.05)
            return OutlierHandlingStrategy.ReplaceWithMedian;
        else if (consensusOutliers > 0)
            return OutlierHandlingStrategy.Flag;
        else
            return OutlierHandlingStrategy.Flag;
    }

    private (double Lower, double Upper) CalculatePercentiles(List<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var lowerIndex = (int)(sorted.Count * _config.CappingPercentile);
        var upperIndex = (int)(sorted.Count * (1.0 - _config.CappingPercentile));
        
        return (sorted[lowerIndex], sorted[Math.Min(upperIndex, sorted.Count - 1)]);
    }

    private double CalculateQualityScore(List<DataQualityIssue> issues, Dictionary<string, double> metrics, int dataPointCount)
    {
        if (dataPointCount == 0) return 0;
        
        var baseScore = 100.0;
        
        foreach (var issue in issues)
        {
            var impact = issue.Severity switch
            {
                IssueSeverity.Critical => 30,
                IssueSeverity.Error => 20,
                IssueSeverity.Warning => 10,
                IssueSeverity.Info => 5,
                _ => 0
            };
            
            baseScore -= impact * issue.ImpactScore;
        }
        
        return Math.Max(0, Math.Min(100, baseScore));
    }

    #endregion
}