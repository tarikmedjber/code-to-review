using MedjCap.Data.Configuration;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using Microsoft.Extensions.Options;

namespace MedjCap.Data.Services;

/// <summary>
/// Pure calculation engine for analysis operations without orchestration concerns.
/// Extracted from AnalysisEngine to follow Single Responsibility Principle.
/// Implements actual calculation logic for correlation, optimization, and validation.
/// </summary>
public class AnalysisCalculationEngine : IAnalysisCalculationEngine
{
    private readonly ICorrelationService _correlationService;
    private readonly IMLBoundaryOptimizer _mlOptimizer;
    private readonly IBacktestService _backtestService;
    private readonly IDataCollector _dataCollector;
    private readonly AnalysisConfig _config;

    public AnalysisCalculationEngine(
        ICorrelationService correlationService,
        IMLBoundaryOptimizer mlOptimizer,
        IBacktestService backtestService,
        IDataCollector dataCollector,
        IOptions<AnalysisConfig> config)
    {
        _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));
        _mlOptimizer = mlOptimizer ?? throw new ArgumentNullException(nameof(mlOptimizer));
        _backtestService = backtestService ?? throw new ArgumentNullException(nameof(backtestService));
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Performs correlation calculations for a single measurement analysis.
    /// </summary>
    public async Task<CorrelationCalculationResult> CalculateCorrelationsAsync(
        AnalysisRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Get time series data from data collector
        var timeSeriesData = await Task.FromResult(_dataCollector.GetTimeSeriesData());
        
        if (!timeSeriesData.DataPoints.Any())
        {
            throw new InvalidOperationException("No data points available for correlation analysis. Ensure data has been collected first.");
        }

        var correlationResults = new Dictionary<TimeSpan, CorrelationResult>();
        var allPriceMovements = new List<PriceMovement>();
        var filteredDataPoints = timeSeriesData.DataPoints.ToList();

        // Calculate correlations for each requested time horizon
        foreach (var timeHorizon in request.TimeHorizons)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Calculate price movements for this time horizon
            var priceMovements = _correlationService.CalculatePriceMovements(timeSeriesData, timeHorizon);
            allPriceMovements.AddRange(priceMovements);

            if (priceMovements.Count < 20) // Minimum sample size for meaningful correlation
            {
                // Log warning about insufficient data but continue with available data
                continue;
            }

            // Calculate correlation for this time horizon
            var correlationType = CorrelationType.Pearson; // Default to Pearson correlation
            var correlationResult = _correlationService.CalculateCorrelation(priceMovements, correlationType);
            
            correlationResults[timeHorizon] = correlationResult;
        }

        // Additional range analysis could be added here in the future

        return new CorrelationCalculationResult
        {
            CorrelationsByHorizon = correlationResults,
            PriceMovements = allPriceMovements,
            FilteredDataPoints = filteredDataPoints,
            TimeSeriesData = timeSeriesData
        };
    }

    /// <summary>
    /// Performs ML boundary optimization calculations.
    /// </summary>
    public async Task<BoundaryOptimizationResult> OptimizeBoundariesAsync(
        AnalysisRequest request,
        CorrelationCalculationResult correlationResults,
        CancellationToken cancellationToken = default)
    {
        if (!correlationResults.PriceMovements.Any())
        {
            throw new ArgumentException("No price movements available for boundary optimization.", nameof(correlationResults));
        }

        var targetATRMove = 2.0m; // Default target ATR move
        var maxRanges = 5; // Default max ranges
        var priceMovements = correlationResults.PriceMovements;

        // Perform basic boundary optimization
        var optimalBoundaries = await Task.FromResult(
            _mlOptimizer.FindOptimalBoundaries(priceMovements, targetATRMove, maxRanges));

        var optimizationMetrics = new Dictionary<string, double>
        {
            ["TotalDataPoints"] = priceMovements.Count,
            ["TargetATRMove"] = (double)targetATRMove,
            ["BoundariesFound"] = optimalBoundaries.Count
        };

        // Determine best method and perform cross-validation if enabled
        string bestMethod = "BasicOptimization";
        CrossValidationResult? crossValidationResults = null;

        if (priceMovements.Count >= 100) // Enable ML optimization for sufficient data
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Create ML optimization configuration
                var mlConfig = new MLOptimizationConfig
                {
                    UseDecisionTree = true,
                    UseClustering = priceMovements.Count > 200,
                    UseGradientSearch = priceMovements.Count > 500,
                    TargetATRMove = targetATRMove,
                    MaxRanges = maxRanges,
                    ValidationRatio = 0.2,
                    MaxIterations = Math.Min(100, priceMovements.Count / 10),
                    ConvergenceThreshold = 0.001
                };

                // Run combined ML optimization
                var combinedResult = await Task.FromResult(
                    _mlOptimizer.RunCombinedOptimization(priceMovements, mlConfig));
                
                if (combinedResult.OptimalBoundaries.Any())
                {
                    optimalBoundaries = combinedResult.OptimalBoundaries;
                    bestMethod = combinedResult.BestMethod ?? "CombinedML";
                    
                    // Store optimization metrics
                    optimizationMetrics["MLOptimizationComplete"] = 1.0;
                    
                    // Cross-validation could be added here
                    // crossValidationResults = _mlOptimizer.KFoldCrossValidation(priceMovements, 5);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                // Fall back to basic optimization if ML optimization fails
                optimizationMetrics["MLOptimizationError"] = 1.0;
                optimizationMetrics["ErrorMessage"] = ex.Message.Length;
                bestMethod = "BasicOptimization_FallBack";
            }
        }

        // Calculate overall performance metrics
        if (optimalBoundaries.Any())
        {
            var avgHitRate = optimalBoundaries.Average(b => b.HitRate);
            optimizationMetrics["AverageHitRate"] = avgHitRate;
            optimizationMetrics["BoundaryCount"] = optimalBoundaries.Count;
        }

        return new BoundaryOptimizationResult
        {
            OptimalBoundaries = optimalBoundaries,
            BestMethod = bestMethod,
            CrossValidationResults = crossValidationResults,
            OptimizationMetrics = optimizationMetrics
        };
    }

    /// <summary>
    /// Performs walk-forward validation calculations.
    /// </summary>
    public async Task<WalkForwardCalculationResult> ValidateWalkForwardAsync(
        AnalysisRequest request,
        BoundaryOptimizationResult boundaryResults,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Make method async
        
        if (!boundaryResults.OptimalBoundaries.Any())
        {
            return new WalkForwardCalculationResult
            {
                WalkForwardResults = new WalkForwardResults(),
                ValidationWindows = new List<WalkForwardWindow>(),
                PerformanceMetrics = new Dictionary<string, double> { ["NoBoundariesError"] = 1.0 }
            };
        }

        // Simplified implementation - validate boundaries on available data
        var timeSeriesData = _dataCollector.GetTimeSeriesData();
        var dataPoints = timeSeriesData.DataPoints.ToList();
        
        if (dataPoints.Count < 100)
        {
            return new WalkForwardCalculationResult
            {
                WalkForwardResults = new WalkForwardResults(),
                ValidationWindows = new List<WalkForwardWindow>(),
                PerformanceMetrics = new Dictionary<string, double> { ["InsufficientDataError"] = 1.0 }
            };
        }

        try
        {
            // Create simple price movements for validation
            var priceMovements = _correlationService.CalculatePriceMovements(timeSeriesData, TimeSpan.FromMinutes(15));
            
            if (priceMovements.Any())
            {
                // Validate the boundaries
                var validationResult = _mlOptimizer.ValidateBoundaries(boundaryResults.OptimalBoundaries, priceMovements);
                
                var performanceMetrics = new Dictionary<string, double>
                {
                    ["ValidationHitRate"] = validationResult.OutOfSamplePerformance,
                    ["InSamplePerformance"] = validationResult.InSamplePerformance,
                    ["PerformanceDegradation"] = validationResult.PerformanceDegradation,
                    ["TotalDataPoints"] = priceMovements.Count,
                    ["BoundariesTested"] = boundaryResults.OptimalBoundaries.Count,
                    ["ValidationComplete"] = 1.0
                };
                
                return new WalkForwardCalculationResult
                {
                    WalkForwardResults = new WalkForwardResults(),
                    ValidationWindows = new List<WalkForwardWindow>(),
                    PerformanceMetrics = performanceMetrics
                };
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            return new WalkForwardCalculationResult
            {
                WalkForwardResults = new WalkForwardResults(),
                ValidationWindows = new List<WalkForwardWindow>(),
                PerformanceMetrics = new Dictionary<string, double> 
                { 
                    ["ValidationError"] = 1.0,
                    ["ErrorCode"] = ex.HResult 
                }
            };
        }
        
        return new WalkForwardCalculationResult
        {
            WalkForwardResults = new WalkForwardResults(),
            ValidationWindows = new List<WalkForwardWindow>(),
            PerformanceMetrics = new Dictionary<string, double> { ["NoValidationPerformed"] = 1.0 }
        };
    }

    /// <summary>
    /// Performs multi-measurement analysis calculations combining multiple indicators.
    /// </summary>
    public async Task<MultiMeasurementCalculationResult> CalculateMultiMeasurementAsync(
        MultiMeasurementAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MeasurementIds?.Length < 2)
        {
            throw new ArgumentException("Multi-measurement analysis requires at least 2 measurement IDs.", nameof(request));
        }

        var timeSeriesData = _dataCollector.GetTimeSeriesData();
        var allDataPoints = timeSeriesData.DataPoints.ToList();
        
        if (!allDataPoints.Any())
        {
            throw new InvalidOperationException("No data points available for multi-measurement analysis.");
        }

        var optimalWeights = new Dictionary<string, double>();
        var individualCorrelations = new Dictionary<string, double>();
        var measurementImportance = new Dictionary<string, double>();
        var allBoundaries = new List<OptimalBoundary>();
        
        var timeHorizon = TimeSpan.FromMinutes(15); // Default time horizon
        var targetATRMove = 2.0m; // Default target ATR move
        
        // Calculate individual correlations for each measurement
        foreach (var measurementId in request.MeasurementIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                // Filter data points for this measurement
                var measurementDataPoints = allDataPoints
                    .Where(dp => dp.MeasurementId == measurementId)
                    .ToList();
                
                if (measurementDataPoints.Count < 20) // Need minimum data
                {
                    individualCorrelations[measurementId] = 0.0;
                    optimalWeights[measurementId] = 0.0;
                    measurementImportance[measurementId] = 0.0;
                    continue;
                }
                
                // Create time series for this measurement
                var measurementTimeSeries = new TimeSeriesData
                {
                    DataPoints = measurementDataPoints,
                    TimeStep = timeHorizon,
                    IsRegular = true
                };
                
                // Calculate price movements and correlation
                var priceMovements = _correlationService.CalculatePriceMovements(measurementTimeSeries, timeHorizon);
                
                if (priceMovements.Any())
                {
                    var correlationResult = _correlationService.CalculateCorrelation(
                        priceMovements, 
                        CorrelationType.Pearson);
                    
                    var correlation = Math.Abs(correlationResult.Coefficient);
                    individualCorrelations[measurementId] = correlation;
                    
                    // Calculate importance based on correlation strength
                    var importance = correlation * (correlationResult.PValue < 0.05 ? 1.0 : 0.5);
                    measurementImportance[measurementId] = importance;
                    
                    // Find boundaries for this measurement if correlation is significant
                    if (correlation > 0.3 && correlationResult.PValue < 0.05)
                    {
                        try
                        {
                            var boundaries = _mlOptimizer.FindOptimalBoundaries(priceMovements, targetATRMove, 3);
                            // Add boundaries (note: MeasurementId tagging would need to be added to OptimalBoundary)
                            allBoundaries.AddRange(boundaries);
                        }
                        catch
                        {
                            // Continue if boundary optimization fails for this measurement
                        }
                    }
                }
                else
                {
                    individualCorrelations[measurementId] = 0.0;
                    measurementImportance[measurementId] = 0.0;
                }
            }
            catch
            {
                // Handle errors gracefully and continue with other measurements
                individualCorrelations[measurementId] = 0.0;
                measurementImportance[measurementId] = 0.0;
            }
        }
        
        // Calculate optimal weights based on importance
        var totalImportance = measurementImportance.Values.Sum();
        
        if (totalImportance > 0)
        {
            foreach (var measurementId in request.MeasurementIds)
            {
                optimalWeights[measurementId] = measurementImportance.GetValueOrDefault(measurementId, 0.0) / totalImportance;
            }
        }
        else
        {
            // Equal weights if no clear importance differences
            var equalWeight = 1.0 / request.MeasurementIds.Length;
            foreach (var measurementId in request.MeasurementIds)
            {
                optimalWeights[measurementId] = equalWeight;
            }
        }
        
        // Calculate combined correlation using weighted average
        var combinedCorrelation = request.MeasurementIds
            .Sum(id => individualCorrelations.GetValueOrDefault(id, 0.0) * optimalWeights.GetValueOrDefault(id, 0.0));
        
        // Filter and rank combined boundaries
        var combinedBoundaries = allBoundaries
            .Where(b => b.HitRate > 0.5) // Only include profitable boundaries
            .OrderByDescending(b => b.HitRate)
            .Take(5) // Default max ranges
            .ToList();
        
        return new MultiMeasurementCalculationResult
        {
            OptimalWeights = optimalWeights,
            IndividualCorrelations = individualCorrelations,
            CombinedCorrelation = combinedCorrelation,
            MeasurementImportance = measurementImportance,
            CombinedBoundaries = combinedBoundaries
        };
    }

    /// <summary>
    /// Performs contextual analysis calculations examining context variable effects.
    /// </summary>
    public async Task<ContextualCalculationResult> CalculateContextualAsync(
        ContextualAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make method async
        
        if (string.IsNullOrEmpty(request.ContextVariable))
        {
            throw new ArgumentException("Context variable must be specified for contextual analysis.", nameof(request));
        }

        // Simplified implementation - returns basic structure
        // Full implementation would require extending domain models
        return new ContextualCalculationResult
        {
            ContextGroups = new List<ContextGroup>(),
            ContextVariable = request.ContextVariable,
            OverallContextEffect = 0.0,
            ContextSignificance = new Dictionary<string, double> 
            { 
                ["ImplementationPending"] = 1.0,
                ["ContextVariable"] = request.ContextVariable.Length
            }
        };
    }
    
    private async Task<List<PriceMovement>> CreatePriceMovementsFromDataPoints(List<DataPoint> dataPoints, TimeSpan timeHorizon)
    {
        // Convert data points to time series format
        var timeSeriesData = new TimeSeriesData 
        { 
            DataPoints = dataPoints,
            TimeStep = TimeSpan.FromMinutes(1), // Assume 1-minute intervals
            IsRegular = true
        };
        
        return await Task.FromResult(_correlationService.CalculatePriceMovements(timeSeriesData, timeHorizon));
    }
    
    private static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (!valuesList.Any()) return 0.0;
        
        var mean = valuesList.Average();
        var sumOfSquares = valuesList.Sum(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(sumOfSquares / valuesList.Count);
    }
}