namespace MedjCap.Data.Domain;

/// <summary>
/// Results from k-fold cross-validation of ML boundary optimization.
/// Provides comprehensive metrics for model selection and overfitting detection.
/// </summary>
public record CrossValidationResult
{
    /// <summary>
    /// Cross-validation performance scores for each fold.
    /// </summary>
    public List<double> FoldScores { get; init; } = new();
    
    /// <summary>
    /// Mean performance score across all folds.
    /// </summary>
    public double MeanScore { get; init; }
    
    /// <summary>
    /// Standard deviation of performance scores across folds.
    /// </summary>
    public double StdDevScore { get; init; }
    
    /// <summary>
    /// Confidence interval for the mean score (95% by default).
    /// </summary>
    public (double Lower, double Upper) ConfidenceInterval { get; init; }
    
    /// <summary>
    /// Detailed results for each cross-validation fold.
    /// </summary>
    public List<CrossValidationFold> FoldResults { get; init; } = new();
    
    /// <summary>
    /// Overall cross-validation metrics and diagnostics.
    /// </summary>
    public Dictionary<string, double> Metrics { get; init; } = new();
    
    /// <summary>
    /// Whether the model shows signs of overfitting based on train/validation gap.
    /// </summary>
    public bool IsOverfitting { get; init; }
    
    /// <summary>
    /// Cross-validation configuration used for this analysis.
    /// </summary>
    public CrossValidationConfig Config { get; init; } = new();
}

/// <summary>
/// Results from a single cross-validation fold.
/// </summary>
public record CrossValidationFold
{
    /// <summary>
    /// Fold index (0-based).
    /// </summary>
    public int FoldIndex { get; init; }
    
    /// <summary>
    /// Training performance score for this fold.
    /// </summary>
    public double TrainingScore { get; init; }
    
    /// <summary>
    /// Validation performance score for this fold.
    /// </summary>
    public double ValidationScore { get; init; }
    
    /// <summary>
    /// Boundaries discovered during training phase of this fold.
    /// </summary>
    public List<OptimalBoundary> TrainingBoundaries { get; init; } = new();
    
    /// <summary>
    /// Performance of training boundaries on validation data.
    /// </summary>
    public ValidationResult ValidationResult { get; init; } = new();
    
    /// <summary>
    /// Number of samples in training set for this fold.
    /// </summary>
    public int TrainingSampleCount { get; init; }
    
    /// <summary>
    /// Number of samples in validation set for this fold.
    /// </summary>
    public int ValidationSampleCount { get; init; }
    
    /// <summary>
    /// Time period covered by this fold (for time-series CV).
    /// </summary>
    public DateRange? Period { get; init; }
}

/// <summary>
/// Configuration for cross-validation methods.
/// </summary>
public record CrossValidationConfig
{
    /// <summary>
    /// Number of folds for k-fold cross-validation.
    /// </summary>
    public int KFolds { get; init; } = 5;
    
    /// <summary>
    /// Cross-validation strategy type.
    /// </summary>
    public CrossValidationStrategy Strategy { get; init; } = CrossValidationStrategy.KFold;
    
    /// <summary>
    /// Random seed for reproducible fold splits (null for random).
    /// </summary>
    public int? RandomSeed { get; init; }
    
    /// <summary>
    /// For time-series CV: minimum training window size as percentage (0.0-1.0).
    /// </summary>
    public double MinimumTrainWindowSize { get; init; } = 0.3;
    
    /// <summary>
    /// For expanding window CV: step size as percentage of total data (0.0-1.0).
    /// </summary>
    public double StepSize { get; init; } = 0.1;
    
    /// <summary>
    /// Performance metric to optimize during cross-validation.
    /// </summary>
    public CrossValidationMetric Metric { get; init; } = CrossValidationMetric.HitRate;
    
    /// <summary>
    /// Confidence level for confidence intervals (e.g., 0.95 for 95%).
    /// </summary>
    public double ConfidenceLevel { get; init; } = 0.95;
}

/// <summary>
/// Cross-validation strategy types for different data characteristics.
/// </summary>
public enum CrossValidationStrategy
{
    /// <summary>
    /// Standard k-fold cross-validation (assumes i.i.d. data).
    /// </summary>
    KFold,
    
    /// <summary>
    /// Time-series cross-validation with expanding training window.
    /// </summary>
    TimeSeriesExpanding,
    
    /// <summary>
    /// Time-series cross-validation with rolling/sliding training window.
    /// </summary>
    TimeSeriesRolling,
    
    /// <summary>
    /// Stratified k-fold preserving class distribution (for categorical targets).
    /// </summary>
    StratifiedKFold,
    
    /// <summary>
    /// Leave-one-out cross-validation (use sparingly - computationally expensive).
    /// </summary>
    LeaveOneOut
}

/// <summary>
/// Performance metrics for cross-validation optimization.
/// </summary>
public enum CrossValidationMetric
{
    /// <summary>
    /// Hit rate: percentage of predictions that exceed target ATR move.
    /// </summary>
    HitRate,
    
    /// <summary>
    /// Mean absolute error of ATR move predictions.
    /// </summary>
    MeanAbsoluteError,
    
    /// <summary>
    /// Root mean squared error of ATR move predictions.
    /// </summary>
    RootMeanSquaredError,
    
    /// <summary>
    /// Sharpe ratio of predicted returns.
    /// </summary>
    SharpeRatio,
    
    /// <summary>
    /// Maximum drawdown of predicted strategy.
    /// </summary>
    MaxDrawdown,
    
    /// <summary>
    /// Information ratio (excess return per unit of tracking error).
    /// </summary>
    InformationRatio
}

/// <summary>
/// Results from time-series cross-validation with temporal structure preservation.
/// </summary>
public record TimeSeriesCrossValidationResult : CrossValidationResult
{
    /// <summary>
    /// Whether the time-series shows evidence of stationarity.
    /// </summary>
    public bool IsStationary { get; init; }
    
    /// <summary>
    /// Results of stationarity test (e.g., ADF test p-value).
    /// </summary>
    public Dictionary<string, double> StationarityTests { get; init; } = new();
    
    /// <summary>
    /// Performance degradation over time (indicates concept drift).
    /// </summary>
    public double TemporalDegradation { get; init; }
    
    /// <summary>
    /// Optimal lookback window size discovered during CV.
    /// </summary>
    public TimeSpan OptimalLookbackWindow { get; init; }
}