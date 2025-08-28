namespace MedjCap.Data.Domain;

/// <summary>
/// Results from combining multiple ML optimization methods.
/// Contains the best method and aggregated results from all approaches.
/// </summary>
public record CombinedOptimizationResult
{
    public string BestMethod { get; init; } = string.Empty;
    public List<OptimalBoundary> OptimalBoundaries { get; init; } = new();
    public double ValidationScore { get; init; }
    public Dictionary<string, MethodResult> MethodResults { get; init; } = new();
    public TimeSpan OptimizationTime { get; init; }
}

/// <summary>
/// Results from a single optimization method within combined optimization.
/// </summary>
public record MethodResult
{
    public List<OptimalBoundary> Boundaries { get; init; } = new();
    public double Score { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
}