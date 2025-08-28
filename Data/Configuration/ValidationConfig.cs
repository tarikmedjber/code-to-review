namespace MedjCap.Data.Configuration;

/// <summary>
/// Configuration for cross-validation and walk-forward analysis
/// </summary>
public class ValidationConfig
{
    /// <summary>
    /// Default number of folds for k-fold cross-validation
    /// </summary>
    public int DefaultKFolds { get; set; } = 5;

    /// <summary>
    /// Default train-test split ratio (80% training, 20% testing)
    /// </summary>
    public double TrainTestSplit { get; set; } = 0.8;

    /// <summary>
    /// Minimum window size for time-series validation
    /// </summary>
    public int MinWindowSize { get; set; } = 50;

    /// <summary>
    /// Maximum number of walk-forward windows
    /// </summary>
    public int MaxWalkForwardWindows { get; set; } = 100;

    /// <summary>
    /// Walk-forward window configuration
    /// </summary>
    public WalkForwardConfig WalkForward { get; set; } = new();

    /// <summary>
    /// Out-of-sample correlation thresholds
    /// </summary>
    public OutOfSampleConfig OutOfSample { get; set; } = new();

    /// <summary>
    /// Backtesting configuration
    /// </summary>
    public BacktestConfig Backtest { get; set; } = new();
}

public class WalkForwardConfig
{
    /// <summary>
    /// Training window percentage (60%)
    /// </summary>
    public double TrainingWindowPercentage { get; set; } = 0.6;

    /// <summary>
    /// Testing window percentage (40%)
    /// </summary>
    public double TestingWindowPercentage { get; set; } = 0.4;

    /// <summary>
    /// Window advancement factor (80%)
    /// </summary>
    public double WindowAdvancementFactor { get; set; } = 0.8;
}

public class OutOfSampleConfig
{
    /// <summary>
    /// Minimum threshold for very small correlation values
    /// </summary>
    public double MinimumCorrelationThreshold { get; set; } = 0.01;

    /// <summary>
    /// Random correlation range for insufficient data (0.1-0.3)
    /// </summary>
    public (double Min, double Max) RandomCorrelationRange { get; set; } = (0.1, 0.3);

    /// <summary>
    /// Default correlation when zero or negative
    /// </summary>
    public double DefaultPositiveCorrelation { get; set; } = 0.15;
}

public class BacktestConfig
{
    /// <summary>
    /// Bias score range (-100 to +100)
    /// </summary>
    public (int Low, int High) BiasScoreRange { get; set; } = (-100, 100);

    /// <summary>
    /// Threshold values for bias classification
    /// </summary>
    public BiasThresholds BiasThresholds { get; set; } = new();

    /// <summary>
    /// Default sample count for predictions
    /// </summary>
    public int DefaultSampleCount { get; set; } = 100;

    /// <summary>
    /// Expected move calculation factor (1% of current value)
    /// </summary>
    public double ExpectedMoveCalculationFactor { get; set; } = 0.01;
}

public class BiasThresholds
{
    /// <summary>
    /// High bias threshold (above 70)
    /// </summary>
    public decimal HighBiasThreshold { get; set; } = 70m;

    /// <summary>
    /// Low bias threshold (below 30)
    /// </summary>
    public decimal LowBiasThreshold { get; set; } = 30m;

    /// <summary>
    /// Strong bullish move threshold (> 2 ATR)
    /// </summary>
    public decimal StrongBullishThreshold { get; set; } = 2m;

    /// <summary>
    /// Fallback moves for different bias conditions
    /// </summary>
    public FallbackMoves FallbackMoves { get; set; } = new();
}

public class FallbackMoves
{
    /// <summary>
    /// High bias fallback move
    /// </summary>
    public double HighBias { get; set; } = 1.2;

    /// <summary>
    /// Low bias fallback move
    /// </summary>
    public double LowBias { get; set; } = -0.8;

    /// <summary>
    /// Neutral bias fallback move
    /// </summary>
    public double Neutral { get; set; } = 0.3;
}