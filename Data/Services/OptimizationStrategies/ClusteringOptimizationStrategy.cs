using MedjCap.Data.Configuration;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using Microsoft.Extensions.Options;
using Accord.MachineLearning;

namespace MedjCap.Data.Services.OptimizationStrategies;

/// <summary>
/// Optimization strategy using k-means clustering to group similar measurement values
/// and identify clusters with high probability of large price movements.
/// </summary>
public class ClusteringOptimizationStrategy : BaseOptimizationStrategy
{
    private readonly int _numberOfClusters;
    private readonly int _maxIterations;
    private readonly double _convergenceThreshold;
    private readonly bool _isEnabled;

    public ClusteringOptimizationStrategy(
        IOptions<OptimizationConfig> optimizationConfig,
        IOptions<MLOptimizationConfig> mlConfig) 
        : base(optimizationConfig)
    {
        var config = mlConfig?.Value ?? throw new ArgumentNullException(nameof(mlConfig));
        _numberOfClusters = Math.Min(GetParameterValue<int>(config.AlgorithmParameters, "ClusterCount") ?? 5, 10); // Cap at 10 clusters
        _maxIterations = GetParameterValue<int>(config.AlgorithmParameters, "ClusterMaxIterations") ?? 100;
        _convergenceThreshold = GetParameterValue<double>(config.AlgorithmParameters, "ClusterConvergenceThreshold") ?? 0.001;
        _isEnabled = config.UseClustering;
    }

    /// <summary>
    /// Name of the optimization strategy for identification and reporting.
    /// </summary>
    public override string StrategyName => "Clustering";

    /// <summary>
    /// Indicates whether this strategy is enabled and should be used.
    /// </summary>
    public override bool IsEnabled => _isEnabled;

    /// <summary>
    /// Gets strategy-specific configuration parameters.
    /// </summary>
    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            ["NumberOfClusters"] = _numberOfClusters,
            ["MaxIterations"] = _maxIterations,
            ["ConvergenceThreshold"] = _convergenceThreshold,
            ["IsEnabled"] = _isEnabled,
            ["Algorithm"] = "K-Means Clustering"
        };
    }

    /// <summary>
    /// Gets the minimum sample size required for clustering optimization.
    /// </summary>
    protected override int GetMinimumSampleSize() => _numberOfClusters * 5; // At least 5 samples per cluster

    /// <summary>
    /// Gets the recommended sample size for optimal clustering performance.
    /// </summary>
    protected override int GetRecommendedSampleSize() => _numberOfClusters * 20; // 20 samples per cluster for robustness

    /// <summary>
    /// Performs clustering-specific validation.
    /// </summary>
    protected override void PerformStrategySpecificValidation(
        List<PriceMovement> trainingData, 
        MLOptimizationConfig config,
        List<string> errors, 
        List<string> warnings, 
        Dictionary<string, object> context)
    {
        // Check if we have enough clusters relative to data size
        var effectiveClusterCount = Math.Min(_numberOfClusters, trainingData.Count / 5);
        if (effectiveClusterCount < 2)
        {
            errors.Add("Insufficient data for meaningful clustering - need at least 2 clusters with minimum 5 samples each");
        }
        else if (effectiveClusterCount < _numberOfClusters)
        {
            warnings.Add($"Reducing cluster count from {_numberOfClusters} to {effectiveClusterCount} due to limited data");
        }

        // Check data distribution for clustering suitability
        var measurementValues = trainingData.Select(m => m.MeasurementValue).OrderBy(x => x).ToList();
        var dataRange = measurementValues.Last() - measurementValues.First();
        
        if (dataRange == 0)
        {
            errors.Add("All measurement values are identical - clustering cannot create meaningful groups");
        }

        // Check for data sparsity that might affect clustering quality
        var quartiles = new[]
        {
            measurementValues[(int)(measurementValues.Count * 0.25)],
            measurementValues[(int)(measurementValues.Count * 0.50)],
            measurementValues[(int)(measurementValues.Count * 0.75)]
        };
        
        var iqr = quartiles[2] - quartiles[0];
        if (iqr == 0)
        {
            warnings.Add("Data has very limited spread (IQR = 0) - clustering may not be effective");
        }

        context["EffectiveClusterCount"] = effectiveClusterCount;
        context["DataRange"] = dataRange;
        context["InterquartileRange"] = iqr;
    }

    /// <summary>
    /// Executes k-means clustering optimization to find optimal boundaries.
    /// </summary>
    protected override List<OptimalBoundary> ExecuteOptimization(
        List<PriceMovement> trainingData, 
        MLOptimizationConfig config, 
        List<string> diagnostics)
    {
        diagnostics.Add($"Starting k-means clustering with {_numberOfClusters} clusters");

        // Adjust cluster count based on available data
        var effectiveClusterCount = Math.Min(_numberOfClusters, trainingData.Count / 5);
        effectiveClusterCount = Math.Max(2, effectiveClusterCount); // At least 2 clusters
        
        diagnostics.Add($"Using {effectiveClusterCount} clusters for {trainingData.Count} data points");

        // Prepare input data (measurement values as 1D vectors)
        var inputs = trainingData.Select(m => new[] { (double)m.MeasurementValue }).ToArray();

        // Create and configure k-means clustering
        var kmeans = new KMeans(effectiveClusterCount)
        {
            MaxIterations = _maxIterations,
            Tolerance = _convergenceThreshold
        };

        // Perform clustering
        var clusters = kmeans.Learn(inputs);
        var labels = clusters.Decide(inputs);
        
        diagnostics.Add($"Clustering completed with {clusters.Centroids.Length} centroids after {kmeans.Iterations} iterations");

        // Analyze cluster performance and create boundaries
        var boundaries = AnalyzeClustersAndCreateBoundaries(trainingData, labels, kmeans, config.TargetATRMove, diagnostics);
        
        diagnostics.Add($"Generated {boundaries.Count} optimal boundaries from clustering analysis");

        return boundaries;
    }

    /// <summary>
    /// Analyzes cluster performance and creates optimal boundaries.
    /// </summary>
    private List<OptimalBoundary> AnalyzeClustersAndCreateBoundaries(
        List<PriceMovement> trainingData,
        int[] labels,
        KMeans kmeans,
        decimal targetATRMove,
        List<string> diagnostics)
    {
        var boundaries = new List<OptimalBoundary>();
        var clusterCount = kmeans.Centroids.Length;

        // Group data by cluster
        var clusterGroups = new Dictionary<int, List<PriceMovement>>();
        for (int i = 0; i < trainingData.Count; i++)
        {
            var clusterLabel = labels[i];
            if (!clusterGroups.ContainsKey(clusterLabel))
                clusterGroups[clusterLabel] = new List<PriceMovement>();
            
            clusterGroups[clusterLabel].Add(trainingData[i]);
        }

        diagnostics.Add($"Data grouped into {clusterGroups.Count} non-empty clusters");

        // Analyze each cluster for boundary creation
        foreach (var kvp in clusterGroups)
        {
            var clusterLabel = kvp.Key;
            var clusterData = kvp.Value;
            
            if (clusterData.Count < 5) // Skip clusters with too few samples
            {
                diagnostics.Add($"Skipping cluster {clusterLabel} - insufficient samples ({clusterData.Count})");
                continue;
            }

            // Calculate cluster statistics
            var measurementValues = clusterData.Select(m => m.MeasurementValue).OrderBy(x => x).ToList();
            var lowerBound = measurementValues.First();
            var upperBound = measurementValues.Last();
            var centroid = (decimal)kmeans.Centroids[clusterLabel][0];

            // Calculate performance metrics
            var largeMoves = clusterData.Count(m => Math.Abs(m.ATRMovement) >= targetATRMove);
            var hitRate = (double)largeMoves / clusterData.Count;
            
            // Calculate cluster quality metrics
            var averageDistance = CalculateAverageDistanceFromCentroid(clusterData, centroid);
            var clusterDensity = clusterData.Count / Math.Max(1, (double)(upperBound - lowerBound));

            diagnostics.Add($"Cluster {clusterLabel}: {clusterData.Count} samples, hit rate {hitRate:F3}, avg distance {averageDistance:F2}");

            // Only create boundaries for clusters with above-average performance
            if (hitRate > 0.1 && clusterData.Count >= 5) // Configurable thresholds
            {
                boundaries.Add(new OptimalBoundary
                {
                    RangeLow = lowerBound,
                    RangeHigh = upperBound,
                    HitRate = hitRate,
                    SampleCount = clusterData.Count,
                    Confidence = CalculateClusterConfidence(clusterData.Count, hitRate, averageDistance, clusterDensity),
                    Method = "K-Means Clustering"
                });
            }
        }

        return boundaries.OrderBy(b => b.RangeLow).ToList();
    }

    /// <summary>
    /// Calculates the average distance of cluster points from the centroid.
    /// </summary>
    private double CalculateAverageDistanceFromCentroid(List<PriceMovement> clusterData, decimal centroid)
    {
        if (!clusterData.Any()) return 0.0;
        
        var totalDistance = clusterData.Sum(m => Math.Abs((double)(m.MeasurementValue - centroid)));
        return totalDistance / clusterData.Count;
    }

    /// <summary>
    /// Calculates confidence level for a cluster-based boundary.
    /// </summary>
    private double CalculateClusterConfidence(int sampleSize, double hitRate, double averageDistance, double density)
    {
        // Multi-factor confidence calculation
        var sizeConfidence = Math.Min(1.0, sampleSize / 50.0); // More samples = higher confidence
        var performanceConfidence = Math.Min(1.0, hitRate * 2.0); // Better hit rate = higher confidence
        var cohesionConfidence = Math.Max(0.1, 1.0 / (1.0 + averageDistance)); // Lower distance = higher confidence
        var densityConfidence = Math.Min(1.0, density / 10.0); // Higher density = higher confidence
        
        // Weighted average of confidence factors
        var weights = new[] { 0.4, 0.3, 0.2, 0.1 }; // Size and performance are most important
        var confidences = new[] { sizeConfidence, performanceConfidence, cohesionConfidence, densityConfidence };
        
        return weights.Zip(confidences, (w, c) => w * c).Sum();
    }

    /// <summary>
    /// Gets a parameter value from the algorithm parameters dictionary.
    /// </summary>
    private T? GetParameterValue<T>(Dictionary<string, object> parameters, string key) where T : struct
    {
        if (parameters.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return null;
    }
}