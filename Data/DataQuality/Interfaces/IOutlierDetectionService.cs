using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;

namespace MedjCap.Data.DataQuality.Interfaces;

/// <summary>
/// Service interface for detecting and handling outliers in statistical analysis.
/// Supports multiple detection methods and handling strategies.
/// </summary>
public interface IOutlierDetectionService
{
    /// <summary>
    /// Detects outliers in a collection of price movements using specified method.
    /// </summary>
    OutlierDetectionResult DetectOutliers(List<PriceMovement> data, OutlierDetectionMethod method);

    /// <summary>
    /// Applies outlier handling strategy to the data based on detection results.
    /// </summary>
    List<PriceMovement> HandleOutliers(List<PriceMovement> data, OutlierDetectionResult detectionResult, OutlierHandlingStrategy strategy);

    /// <summary>
    /// Gets comprehensive outlier analysis including multiple detection methods.
    /// </summary>
    OutlierAnalysisResult AnalyzeOutliers(List<PriceMovement> data);

    /// <summary>
    /// Validates data quality and provides outlier impact assessment.
    /// </summary>
    DataQualityReport AssessDataQuality(List<PriceMovement> data);
}