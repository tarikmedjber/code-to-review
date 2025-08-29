using MedjCap.Data.Trading.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.MachineLearning.Models;
using MedjCap.Data.Statistics.Models;
using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.MachineLearning.Optimization.Models;

namespace MedjCap.Data.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when ML optimization algorithms fail to converge within specified limits.
/// Provides detailed convergence metrics and suggestions for algorithm tuning.
/// </summary>
public class OptimizationConvergenceException : MedjCapException
{
    /// <summary>
    /// Number of iterations completed before stopping
    /// </summary>
    public int CompletedIterations { get; }

    /// <summary>
    /// Maximum iterations allowed
    /// </summary>
    public int MaxIterations { get; }

    /// <summary>
    /// Final error/objective value when stopping
    /// </summary>
    public double FinalError { get; }

    /// <summary>
    /// Target convergence threshold
    /// </summary>
    public double ConvergenceThreshold { get; }

    /// <summary>
    /// Type of optimization algorithm that failed to converge
    /// </summary>
    public OptimizationTarget OptimizationType { get; }

    /// <summary>
    /// Reason why convergence failed
    /// </summary>
    public ConvergenceFailureReason FailureReason { get; }

    /// <summary>
    /// History of error values during optimization (last 10 iterations)
    /// </summary>
    public double[] ErrorHistory { get; }

    public OptimizationConvergenceException(
        OptimizationTarget optimizationType,
        int completedIterations,
        int maxIterations,
        double finalError,
        double convergenceThreshold,
        ConvergenceFailureReason failureReason,
        double[]? errorHistory = null,
        string? additionalContext = null,
        Exception? innerException = null)
        : base(
            errorCode: "OPTIMIZATION_CONVERGENCE_FAILED",
            message: $"{optimizationType} optimization failed to converge after {completedIterations} iterations",
            userMessage: CreateUserMessage(optimizationType, completedIterations, maxIterations, finalError, convergenceThreshold, failureReason),
            context: CreateContext(optimizationType, completedIterations, maxIterations, finalError, convergenceThreshold, failureReason, errorHistory, additionalContext),
            innerException: innerException)
    {
        CompletedIterations = completedIterations;
        MaxIterations = maxIterations;
        FinalError = finalError;
        ConvergenceThreshold = convergenceThreshold;
        OptimizationType = optimizationType;
        FailureReason = failureReason;
        ErrorHistory = errorHistory?.TakeLast(10).ToArray() ?? Array.Empty<double>();
    }

    private static string CreateUserMessage(
        OptimizationTarget type,
        int iterations,
        int maxIterations,
        double finalError,
        double threshold,
        ConvergenceFailureReason reason)
    {
        var baseMessage = $"{type} optimization did not converge after {iterations} iterations (max: {maxIterations}).";
        
        return reason switch
        {
            ConvergenceFailureReason.MaxIterationsReached => 
                $"{baseMessage} Final error {finalError:F6} still above threshold {threshold:F6}. Consider increasing max iterations or relaxing convergence criteria.",
            
            ConvergenceFailureReason.ErrorIncreasing => 
                $"{baseMessage} Error started increasing, suggesting algorithm instability. Try reducing step size or changing optimization method.",
            
            ConvergenceFailureReason.NoImprovement => 
                $"{baseMessage} No improvement detected for several iterations. The algorithm may be stuck in local optimum.",
            
            ConvergenceFailureReason.NumericalInstability => 
                $"{baseMessage} Numerical instability detected. Consider data normalization or algorithm regularization.",
            
            ConvergenceFailureReason.InvalidObjectiveFunction => 
                $"{baseMessage} Objective function returned invalid values. Check data quality and optimization parameters.",
            
            _ => baseMessage
        };
    }

    private static Dictionary<string, object> CreateContext(
        OptimizationTarget type,
        int iterations,
        int maxIterations,
        double finalError,
        double threshold,
        ConvergenceFailureReason reason,
        double[]? errorHistory,
        string? additionalContext)
    {
        var context = new Dictionary<string, object>
        {
            ["OptimizationType"] = type.ToString(),
            ["CompletedIterations"] = iterations,
            ["MaxIterations"] = maxIterations,
            ["FinalError"] = finalError,
            ["ConvergenceThreshold"] = threshold,
            ["FailureReason"] = reason.ToString(),
            ["ErrorRatio"] = threshold > 0 ? finalError / threshold : double.PositiveInfinity,
            ["IterationProgress"] = maxIterations > 0 ? (double)iterations / maxIterations : 1.0,
            ["AdditionalContext"] = additionalContext ?? "None"
        };

        if (errorHistory?.Any() == true)
        {
            context["ErrorHistory"] = errorHistory.TakeLast(5).ToArray();
            context["ErrorTrend"] = CalculateErrorTrend(errorHistory);
            context["BestError"] = errorHistory.Min();
            context["WorstError"] = errorHistory.Max();
        }

        return context;
    }

    private static string CalculateErrorTrend(double[] errorHistory)
    {
        if (errorHistory.Length < 2) return "Unknown";
        
        var recent = errorHistory.TakeLast(Math.Min(5, errorHistory.Length)).ToArray();
        if (recent.Length < 2) return "Unknown";
        
        var trend = recent.Last() - recent.First();
        return trend switch
        {
            > 0.01 => "Increasing",
            < -0.01 => "Decreasing", 
            _ => "Stable"
        };
    }

    /// <summary>
    /// Gets optimization tuning suggestions based on the failure reason
    /// </summary>
    public string[] GetTuningSuggestions()
    {
        return FailureReason switch
        {
            ConvergenceFailureReason.MaxIterationsReached => new[]
            {
                "Increase MaxIterations in OptimizationConfig",
                "Relax ConvergenceThreshold for faster convergence",
                "Try different optimization algorithm",
                "Improve initial parameter estimates"
            },
            
            ConvergenceFailureReason.ErrorIncreasing => new[]
            {
                "Reduce learning rate or step size",
                "Add regularization to prevent overfitting",
                "Check for data quality issues",
                "Use more stable optimization algorithm"
            },
            
            ConvergenceFailureReason.NoImprovement => new[]
            {
                "Try random restart with different initial conditions",
                "Use global optimization methods",
                "Increase exploration in the algorithm",
                "Check if local optimum is acceptable"
            },
            
            ConvergenceFailureReason.NumericalInstability => new[]
            {
                "Normalize input data to prevent numerical issues",
                "Use double precision arithmetic",
                "Add numerical stability checks",
                "Apply gradient clipping if using gradient methods"
            },
            
            ConvergenceFailureReason.InvalidObjectiveFunction => new[]
            {
                "Validate input data for NaN or infinite values",
                "Check objective function implementation",
                "Add bounds to optimization parameters",
                "Use robust optimization methods"
            },
            
            _ => new[] { "Review optimization setup and data quality" }
        };
    }
}

/// <summary>
/// Specific reasons why optimization convergence can fail
/// </summary>
public enum ConvergenceFailureReason
{
    MaxIterationsReached,
    ErrorIncreasing,
    NoImprovement,
    NumericalInstability,
    InvalidObjectiveFunction,
    AlgorithmError,
    UnknownError
}