using MedjCap.Data.Domain;

namespace MedjCap.Data.Core;

/// <summary>
/// Formats analysis results into different output formats for consumption.
/// Separated from core analysis logic to allow multiple presentation formats.
/// </summary>
public interface IAnalysisResultFormatter
{
    /// <summary>
    /// Formats analysis results as a tabular output for display.
    /// </summary>
    /// <param name="result">Analysis results to format</param>
    /// <param name="options">Formatting options</param>
    /// <returns>Formatted table output</returns>
    TableOutput FormatAsTable(AnalysisResult result, TableFormatOptions? options = null);
    
    /// <summary>
    /// Formats analysis results as a comprehensive statistical report.
    /// </summary>
    /// <param name="result">Analysis results to format</param>
    /// <param name="options">Report formatting options</param>
    /// <returns>Statistical report</returns>
    StatisticalReport FormatAsReport(AnalysisResult result, ReportFormatOptions? options = null);
    
    /// <summary>
    /// Formats analysis results as a predictive model summary.
    /// </summary>
    /// <param name="result">Analysis results to format</param>
    /// <param name="options">Model formatting options</param>
    /// <returns>Predictive model summary</returns>
    PredictiveModel FormatAsPredictiveModel(AnalysisResult result, ModelFormatOptions? options = null);
    
    /// <summary>
    /// Formats analysis results as JSON for API consumption.
    /// </summary>
    /// <param name="result">Analysis results to format</param>
    /// <param name="options">JSON formatting options</param>
    /// <returns>JSON representation</returns>
    string FormatAsJson(AnalysisResult result, JsonFormatOptions? options = null);
    
    /// <summary>
    /// Formats multi-measurement analysis results.
    /// </summary>
    /// <param name="result">Multi-measurement analysis results</param>
    /// <param name="options">Formatting options</param>
    /// <returns>Formatted output</returns>
    TableOutput FormatMultiMeasurementTable(MultiMeasurementAnalysisResult result, TableFormatOptions? options = null);
    
    /// <summary>
    /// Formats contextual analysis results.
    /// </summary>
    /// <param name="result">Contextual analysis results</param>
    /// <param name="options">Formatting options</param>
    /// <returns>Formatted output</returns>
    TableOutput FormatContextualTable(ContextualAnalysisResult result, TableFormatOptions? options = null);
}

/// <summary>
/// Tabular output for analysis results.
/// </summary>
public record TableOutput
{
    /// <summary>
    /// Table headers.
    /// </summary>
    public List<string> Headers { get; init; } = new();
    
    /// <summary>
    /// Table rows with data values.
    /// </summary>
    public List<List<object>> Rows { get; init; } = new();
    
    /// <summary>
    /// Column formatting information.
    /// </summary>
    public Dictionary<string, ColumnFormat> ColumnFormats { get; init; } = new();
    
    /// <summary>
    /// Table metadata and summary information.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// Table title and description.
    /// </summary>
    public string Title { get; init; } = string.Empty;
    
    /// <summary>
    /// Footnotes and additional information.
    /// </summary>
    public List<string> Footnotes { get; init; } = new();
}

/// <summary>
/// Statistical report containing detailed analysis narrative.
/// </summary>
public record StatisticalReport
{
    /// <summary>
    /// Executive summary of findings.
    /// </summary>
    public string ExecutiveSummary { get; init; } = string.Empty;
    
    /// <summary>
    /// Detailed methodology description.
    /// </summary>
    public string Methodology { get; init; } = string.Empty;
    
    /// <summary>
    /// Key findings and insights.
    /// </summary>
    public List<Finding> KeyFindings { get; init; } = new();
    
    /// <summary>
    /// Statistical significance analysis.
    /// </summary>
    public string StatisticalAnalysis { get; init; } = string.Empty;
    
    /// <summary>
    /// Risk assessment and limitations.
    /// </summary>
    public string RiskAssessment { get; init; } = string.Empty;
    
    /// <summary>
    /// Recommendations for action.
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
    
    /// <summary>
    /// Appendices with detailed data.
    /// </summary>
    public Dictionary<string, object> Appendices { get; init; } = new();
    
    /// <summary>
    /// Report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Predictive model summary for trading applications.
/// </summary>
public record PredictiveModel
{
    /// <summary>
    /// Model type and description.
    /// </summary>
    public string ModelType { get; init; } = string.Empty;
    
    /// <summary>
    /// Prediction accuracy metrics.
    /// </summary>
    public Dictionary<string, double> AccuracyMetrics { get; init; } = new();
    
    /// <summary>
    /// Input features and their importance.
    /// </summary>
    public Dictionary<string, double> FeatureImportance { get; init; } = new();
    
    /// <summary>
    /// Prediction rules and thresholds.
    /// </summary>
    public List<PredictionRule> PredictionRules { get; init; } = new();
    
    /// <summary>
    /// Model validation results.
    /// </summary>
    public ModelValidation Validation { get; init; } = new();
    
    /// <summary>
    /// Expected performance in live trading.
    /// </summary>
    public PerformanceProjection Performance { get; init; } = new();
    
    /// <summary>
    /// Model usage guidelines and warnings.
    /// </summary>
    public List<string> UsageGuidelines { get; init; } = new();
}

/// <summary>
/// Individual finding from statistical analysis.
/// </summary>
public record Finding
{
    /// <summary>
    /// Finding category (correlation, optimization, validation, etc.).
    /// </summary>
    public string Category { get; init; } = string.Empty;
    
    /// <summary>
    /// Finding description.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Statistical significance level.
    /// </summary>
    public double Significance { get; init; }
    
    /// <summary>
    /// Confidence in the finding.
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Supporting data and metrics.
    /// </summary>
    public Dictionary<string, object> SupportingData { get; init; } = new();
}

/// <summary>
/// Column formatting information for tables.
/// </summary>
public record ColumnFormat
{
    /// <summary>
    /// Data type for the column.
    /// </summary>
    public Type DataType { get; init; } = typeof(object);
    
    /// <summary>
    /// Number format string for numeric columns.
    /// </summary>
    public string? NumberFormat { get; init; }
    
    /// <summary>
    /// Column alignment (left, center, right).
    /// </summary>
    public string Alignment { get; init; } = "left";
    
    /// <summary>
    /// Column width hint for display.
    /// </summary>
    public int Width { get; init; } = 0;
}

/// <summary>
/// Prediction rule for the predictive model.
/// </summary>
public record PredictionRule
{
    /// <summary>
    /// Rule condition (e.g., "RSI > 70").
    /// </summary>
    public string Condition { get; init; } = string.Empty;
    
    /// <summary>
    /// Predicted outcome.
    /// </summary>
    public string Prediction { get; init; } = string.Empty;
    
    /// <summary>
    /// Rule confidence score.
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Historical success rate of the rule.
    /// </summary>
    public double SuccessRate { get; init; }
}

/// <summary>
/// Model validation metrics.
/// </summary>
public record ModelValidation
{
    /// <summary>
    /// Cross-validation results.
    /// </summary>
    public CrossValidationResult? CrossValidation { get; init; }
    
    /// <summary>
    /// Out-of-sample test results.
    /// </summary>
    public Dictionary<string, double> OutOfSampleMetrics { get; init; } = new();
    
    /// <summary>
    /// Walk-forward validation results.
    /// </summary>
    public WalkForwardResults? WalkForward { get; init; }
}

/// <summary>
/// Performance projection for live trading.
/// </summary>
public record PerformanceProjection
{
    /// <summary>
    /// Expected return per trade.
    /// </summary>
    public double ExpectedReturn { get; init; }
    
    /// <summary>
    /// Expected win rate.
    /// </summary>
    public double WinRate { get; init; }
    
    /// <summary>
    /// Risk metrics (Sharpe ratio, max drawdown, etc.).
    /// </summary>
    public Dictionary<string, double> RiskMetrics { get; init; } = new();
    
    /// <summary>
    /// Confidence intervals for projections.
    /// </summary>
    public Dictionary<string, (double Lower, double Upper)> ConfidenceIntervals { get; init; } = new();
}

/// <summary>
/// Options for table formatting.
/// </summary>
public record TableFormatOptions
{
    /// <summary>
    /// Include summary statistics row.
    /// </summary>
    public bool IncludeSummary { get; init; } = true;
    
    /// <summary>
    /// Number of decimal places for numeric values.
    /// </summary>
    public int DecimalPlaces { get; init; } = 4;
    
    /// <summary>
    /// Sort column and direction.
    /// </summary>
    public string? SortColumn { get; init; }
    
    /// <summary>
    /// Maximum number of rows to include.
    /// </summary>
    public int? MaxRows { get; init; }
    
    /// <summary>
    /// Include confidence intervals.
    /// </summary>
    public bool IncludeConfidenceIntervals { get; init; } = true;
}

/// <summary>
/// Options for report formatting.
/// </summary>
public record ReportFormatOptions
{
    /// <summary>
    /// Level of detail (summary, detailed, comprehensive).
    /// </summary>
    public ReportDetailLevel DetailLevel { get; init; } = ReportDetailLevel.Detailed;
    
    /// <summary>
    /// Include technical appendices.
    /// </summary>
    public bool IncludeTechnicalAppendices { get; init; } = true;
    
    /// <summary>
    /// Target audience (technical, business, executive).
    /// </summary>
    public ReportAudience Audience { get; init; } = ReportAudience.Technical;
    
    /// <summary>
    /// Include charts and visualizations.
    /// </summary>
    public bool IncludeVisualizations { get; init; } = false;
}

/// <summary>
/// Options for predictive model formatting.
/// </summary>
public record ModelFormatOptions
{
    /// <summary>
    /// Include model internals and parameters.
    /// </summary>
    public bool IncludeInternals { get; init; } = true;
    
    /// <summary>
    /// Include performance projections.
    /// </summary>
    public bool IncludeProjections { get; init; } = true;
    
    /// <summary>
    /// Confidence level for intervals.
    /// </summary>
    public double ConfidenceLevel { get; init; } = 0.95;
}

/// <summary>
/// Options for JSON formatting.
/// </summary>
public record JsonFormatOptions
{
    /// <summary>
    /// Include null values in output.
    /// </summary>
    public bool IncludeNulls { get; init; } = false;
    
    /// <summary>
    /// Indent JSON for readability.
    /// </summary>
    public bool Indent { get; init; } = false;
    
    /// <summary>
    /// Include metadata and timestamps.
    /// </summary>
    public bool IncludeMetadata { get; init; } = true;
}

/// <summary>
/// Report detail levels.
/// </summary>
public enum ReportDetailLevel
{
    /// <summary>
    /// Summary level with key findings only.
    /// </summary>
    Summary,
    
    /// <summary>
    /// Detailed analysis with methodology.
    /// </summary>
    Detailed,
    
    /// <summary>
    /// Comprehensive report with all appendices.
    /// </summary>
    Comprehensive
}

/// <summary>
/// Report target audiences.
/// </summary>
public enum ReportAudience
{
    /// <summary>
    /// Technical audience with statistical background.
    /// </summary>
    Technical,
    
    /// <summary>
    /// Business audience with trading knowledge.
    /// </summary>
    Business,
    
    /// <summary>
    /// Executive audience focused on outcomes.
    /// </summary>
    Executive
}