namespace MedjCap.Data.Domain;

/// <summary>
/// Represents a solution on the Pareto front for multi-objective optimization.
/// Contains the boundary and scores for each optimization objective.
/// </summary>
public record ParetoSolution
{
    public OptimalBoundary Boundary { get; init; } = new();
    public List<double> Scores { get; init; } = new();
    public bool IsDominated { get; set; }
    public double DominationRank { get; set; }
    public Dictionary<string, double> ObjectiveValues { get; init; } = new();
}