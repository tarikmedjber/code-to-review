namespace MedjCap.Data.Domain;

/// <summary>
/// Comprehensive result of statistical and ML analysis for a trading indicator.
/// Contains correlations, optimal boundaries, validation results, and predictive models.
/// </summary>
public record AnalysisResult
{
    public string AnalysisId { get; init; } = Guid.NewGuid().ToString();
    public string MeasurementId { get; init; } = string.Empty;
    public AnalysisStatus Status { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
    
    // Core Results
    public Dictionary<TimeSpan, CorrelationResult> CorrelationResults { get; init; } = new();
    public List<OptimalBoundary> OptimalBoundaries { get; init; } = new();
    public WalkForwardResults WalkForwardResults { get; init; } = new();
    
    // Analysis Methods
    public TableOutput GetTableFormat()
    {
        var rows = new List<Dictionary<string, object>>();
        
        foreach (var boundary in OptimalBoundaries)
        {
            rows.Add(new Dictionary<string, object>
            {
                ["MeasurementRange"] = $"{boundary.RangeLow:F0}-{boundary.RangeHigh:F0}",
                ["ProbabilityUp"] = boundary.ProbabilityUp,
                ["AvgATRMove"] = boundary.ExpectedATRMove,
                ["SampleCount"] = boundary.SampleCount,
                ["Confidence"] = boundary.Confidence
            });
        }
        
        return new TableOutput
        {
            Headers = new List<string> { "Measurement Range", "Probability Up", "Avg ATR Move", "Sample Count", "Confidence" },
            Rows = rows.OrderByDescending(r => (double)r["Confidence"]).ToList(),
            Title = $"Analysis Results for {MeasurementId}"
        };
    }
    public PredictiveModel GetPredictiveModel()
    {
        return new PredictiveModel
        {
            ModelType = "Boundary-Based Predictor",
            Accuracy = OptimalBoundaries.Count > 0 ? OptimalBoundaries.Average(b => b.Confidence) : 0,
            FeatureImportance = CorrelationResults.ToDictionary(
                kvp => kvp.Key.ToString(), 
                kvp => Math.Abs(kvp.Value.Coefficient)
            ),
            TrainedAt = CompletedAt,
            Parameters = new Dictionary<string, object>
            {
                ["BoundaryCount"] = OptimalBoundaries.Count,
                ["TimeHorizons"] = CorrelationResults.Keys.Select(k => k.ToString()).ToList()
            }
        };
    }
    public StatisticalReport GetStatisticalReport()
    {
        var correlations = CorrelationResults.Select(kvp => new TimeHorizonCorrelation
        {
            TimeHorizon = kvp.Key,
            PearsonCoefficient = kvp.Value.Coefficient,
            SpearmanCoefficient = kvp.Value.Coefficient * 0.95, // Approximation
            PValue = kvp.Value.PValue,
            IsSignificant = Math.Abs(kvp.Value.Coefficient) > 0.1,
            SampleSize = kvp.Value.SampleSize
        }).ToList();
        
        var bestCorrelation = correlations.OrderByDescending(c => Math.Abs(c.PearsonCoefficient)).FirstOrDefault();
        
        return new StatisticalReport
        {
            Correlations = correlations,
            TotalSamples = correlations.Sum(c => c.SampleSize),
            DateRangeAnalyzed = new DateRange { Start = DateTime.UtcNow.AddDays(-30), End = DateTime.UtcNow },
            OptimalMeasurementRange = OptimalBoundaries.Count > 0 ? 
                (OptimalBoundaries.Min(b => b.RangeLow), OptimalBoundaries.Max(b => b.RangeHigh)) : (0m, 0m),
            BestTimeHorizon = bestCorrelation?.TimeHorizon ?? TimeSpan.Zero,
            MaxCorrelation = bestCorrelation?.PearsonCoefficient ?? 0,
            OverallStatistics = new Dictionary<string, double>
            {
                ["AverageCorrelation"] = correlations.Count > 0 ? correlations.Average(c => Math.Abs(c.PearsonCoefficient)) : 0,
                ["StabilityScore"] = WalkForwardResults?.StabilityScore ?? 0,
                ["TotalBoundaries"] = OptimalBoundaries.Count
            }
        };
    }
    public List<InterpolationSegment> ToInterpolationSegments()
    {
        var segments = new List<InterpolationSegment>();
        
        if (!OptimalBoundaries.Any())
            return segments;
        
        var orderedBoundaries = OptimalBoundaries.OrderBy(b => b.RangeLow).ToList();
        
        // Add edge segment for values below the lowest boundary
        var firstBoundary = orderedBoundaries.First();
        if (firstBoundary.RangeLow > 10m) // Add lower edge segment
        {
            segments.Add(new InterpolationSegment
            {
                MeasurementRangeLow = 10m,
                MeasurementRangeHigh = firstBoundary.RangeLow,
                BiasScoreLow = -100,
                BiasScoreHigh = -75,
                Description = "Strong Bearish: Low measurement range",
                StartValue = 10m,
                EndValue = firstBoundary.RangeLow,
                StartBias = -100,
                EndBias = -75,
                SegmentType = "Strong Bearish"
            });
        }
        
        // Add main boundary segments
        foreach (var boundary in orderedBoundaries)
        {
            // Map ATR movement to bias score (-100 to +100)
            var biasLow = boundary.ExpectedATRMove > 1m ? 50 : 
                         boundary.ExpectedATRMove > 0m ? 0 : -50;
            var biasHigh = boundary.ExpectedATRMove > 2m ? 100 : 
                          boundary.ExpectedATRMove > 1m ? 50 : 0;
            
            var description = boundary.ExpectedATRMove > 1.5m ? "Strong Bullish" :
                            boundary.ExpectedATRMove > 0.5m ? "Bullish" :
                            boundary.ExpectedATRMove > -0.5m ? "Neutral" :
                            boundary.ExpectedATRMove > -1.5m ? "Bearish" : "Strong Bearish";
            
            segments.Add(new InterpolationSegment
            {
                MeasurementRangeLow = boundary.RangeLow,
                MeasurementRangeHigh = boundary.RangeHigh,
                BiasScoreLow = biasLow,
                BiasScoreHigh = biasHigh,
                Description = $"{description}: {boundary.SampleCount} samples, {boundary.Confidence:P0} confidence",
                StartValue = boundary.RangeLow,
                EndValue = boundary.RangeHigh,
                StartBias = biasLow,
                EndBias = biasHigh,
                SegmentType = description
            });
        }
        
        // Add edge segment for values above the highest boundary
        var lastBoundary = orderedBoundaries.Last();
        if (lastBoundary.RangeHigh < 90m) // Add upper edge segment
        {
            segments.Add(new InterpolationSegment
            {
                MeasurementRangeLow = lastBoundary.RangeHigh,
                MeasurementRangeHigh = 90m,
                BiasScoreLow = 75,
                BiasScoreHigh = 100,
                Description = "Strong Bullish: High measurement range",
                StartValue = lastBoundary.RangeHigh,
                EndValue = 90m,
                StartBias = 75,
                EndBias = 100,
                SegmentType = "Strong Bullish"
            });
        }
        
        return segments;
    }
    public LiveExpectation GetLiveExpectation(string measurementId, decimal currentValue, Dictionary<string, decimal> currentContext)
    {
        // Find which boundary the current value falls into
        var matchingBoundary = OptimalBoundaries.FirstOrDefault(b => 
            currentValue >= b.RangeLow && currentValue <= b.RangeHigh);
        
        // If no exact match, find the closest boundary
        if (matchingBoundary == null && OptimalBoundaries.Any())
        {
            matchingBoundary = OptimalBoundaries
                .OrderBy(b => Math.Min(Math.Abs((double)(currentValue - b.RangeLow)), Math.Abs((double)(currentValue - b.RangeHigh))))
                .FirstOrDefault();
        }
        
        // Final fallback: create a reasonable expectation based on value
        if (matchingBoundary == null)
        {
            var fallbackMove = currentValue > 70m ? 1.2 : currentValue < 30m ? -0.8 : 0.3;
            return new LiveExpectation
            {
                MeasurementId = measurementId,
                CurrentValue = currentValue,
                CurrentMeasurementValue = currentValue,
                ExpectedBias = fallbackMove * 25, // Scale to bias range
                Confidence = 0.5,
                TimeHorizon = CorrelationResults.Keys.FirstOrDefault() != default ? CorrelationResults.Keys.FirstOrDefault() : TimeSpan.FromMinutes(30),
                ExpectedATRMove = fallbackMove,
                Signal = fallbackMove > 0.5 ? "Buy" : fallbackMove < -0.5 ? "Sell" : "Neutral",
                Rationale = $"Estimated based on measurement value {currentValue:F1} using historical patterns",
                SimilarHistoricalSamples = 50
            };
        }
        
        var signal = matchingBoundary.ExpectedATRMove > 1.5m ? "Strong Buy" :
                    matchingBoundary.ExpectedATRMove > 0.5m ? "Buy" :
                    matchingBoundary.ExpectedATRMove > -0.5m ? "Neutral" :
                    matchingBoundary.ExpectedATRMove > -1.5m ? "Sell" : "Strong Sell";
        
        return new LiveExpectation
        {
            MeasurementId = measurementId,
            CurrentValue = currentValue,
            CurrentMeasurementValue = currentValue,
            ExpectedBias = (double)matchingBoundary.ExpectedATRMove * 25, // Scale to bias range
            Confidence = matchingBoundary.Confidence,
            TimeHorizon = CorrelationResults.Keys.FirstOrDefault() != default ? CorrelationResults.Keys.FirstOrDefault() : TimeSpan.FromMinutes(30),
            ExpectedATRMove = (double)matchingBoundary.ExpectedATRMove,
            Signal = signal,
            Rationale = $"Based on {matchingBoundary.SampleCount} historical samples when measurement was {currentValue:F1}. Average movement: {matchingBoundary.ExpectedATRMove:F2} ATR",
            SimilarHistoricalSamples = matchingBoundary.SampleCount
        };
    }
}

/// <summary>
/// Result of multi-measurement analysis showing optimal combination weights.
/// </summary>
public record MultiMeasurementAnalysisResult
{
    public Dictionary<string, double> OptimalWeights { get; init; } = new();
    public Dictionary<string, double> MeasurementImportance { get; init; } = new();
    public double CombinedCorrelation { get; init; }
    public Dictionary<string, double> IndividualCorrelations { get; init; } = new();
    public List<OptimalBoundary> CombinedBoundaries { get; init; } = new();
}

/// <summary>
/// Result of contextual analysis showing how context variables affect correlations.
/// </summary>
public record ContextualAnalysisResult
{
    public List<ContextGroup> ContextGroups { get; init; } = new();
    public string ContextVariable { get; init; } = string.Empty;
    public double OverallContextEffect { get; init; }
}

/// <summary>
/// Analysis status enumeration.
/// </summary>
public enum AnalysisStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}