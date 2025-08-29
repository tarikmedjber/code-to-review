using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.TimeSeries.Interfaces;

namespace MedjCap.Data.Analysis.Interfaces;

/// <summary>
/// Main analysis engine that orchestrates data collection, correlation analysis, ML optimization, and backtesting.
/// Provides comprehensive statistical analysis capabilities for trading indicator evaluation.
/// </summary>
public interface IAnalysisEngine
{
    Task<AnalysisResult> RunAnalysisAsync(AnalysisRequest request, CancellationToken cancellationToken = default);
    Task<MultiMeasurementAnalysisResult> RunMultiMeasurementAnalysisAsync(MultiMeasurementAnalysisRequest request, CancellationToken cancellationToken = default);
    Task<ContextualAnalysisResult> RunContextualAnalysisAsync(ContextualAnalysisRequest request, CancellationToken cancellationToken = default);
    Task<IDataCollector> GetDataCollectorAsync(CancellationToken cancellationToken = default);
}