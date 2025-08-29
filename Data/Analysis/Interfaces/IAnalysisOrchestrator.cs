using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.TimeSeries.Interfaces;

namespace MedjCap.Data.Analysis.Interfaces;

/// <summary>
/// Orchestrates analysis workflows by coordinating validation, calculation, aggregation, and formatting.
/// Replaces the monolithic AnalysisEngine with a focused orchestration responsibility.
/// </summary>
public interface IAnalysisOrchestrator
{
    /// <summary>
    /// Orchestrates a complete single measurement analysis workflow.
    /// </summary>
    /// <param name="request">Analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete analysis result</returns>
    Task<AnalysisResult> RunAnalysisAsync(AnalysisRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Orchestrates a multi-measurement analysis workflow.
    /// </summary>
    /// <param name="request">Multi-measurement analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multi-measurement analysis result</returns>
    Task<MultiMeasurementAnalysisResult> RunMultiMeasurementAnalysisAsync(
        MultiMeasurementAnalysisRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Orchestrates a contextual analysis workflow.
    /// </summary>
    /// <param name="request">Contextual analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Contextual analysis result</returns>
    Task<ContextualAnalysisResult> RunContextualAnalysisAsync(
        ContextualAnalysisRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the data collector for external access (backward compatibility).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data collector instance</returns>
    Task<IDataCollector> GetDataCollectorAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating analysis workflows based on request types.
/// Enables different workflow strategies for different analysis types.
/// </summary>
public interface IAnalysisWorkflowFactory
{
    /// <summary>
    /// Creates a workflow for the specified analysis type.
    /// </summary>
    /// <param name="analysisType">Type of analysis to create workflow for</param>
    /// <returns>Analysis workflow instance</returns>
    IAnalysisWorkflow CreateWorkflow(AnalysisType analysisType);
    
    /// <summary>
    /// Gets available analysis types supported by the factory.
    /// </summary>
    /// <returns>List of supported analysis types</returns>
    IEnumerable<AnalysisType> GetSupportedAnalysisTypes();
}

/// <summary>
/// Represents a specific analysis workflow with its execution strategy.
/// </summary>
public interface IAnalysisWorkflow
{
    /// <summary>
    /// Analysis type handled by this workflow.
    /// </summary>
    AnalysisType AnalysisType { get; }
    
    /// <summary>
    /// Executes the workflow for a standard analysis request.
    /// </summary>
    /// <param name="request">Analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result</returns>
    Task<AnalysisResult> ExecuteAsync(AnalysisRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes the workflow for a multi-measurement analysis request.
    /// </summary>
    /// <param name="request">Multi-measurement analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multi-measurement analysis result</returns>
    Task<MultiMeasurementAnalysisResult> ExecuteAsync(
        MultiMeasurementAnalysisRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes the workflow for a contextual analysis request.
    /// </summary>
    /// <param name="request">Contextual analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Contextual analysis result</returns>
    Task<ContextualAnalysisResult> ExecuteAsync(
        ContextualAnalysisRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that the workflow can handle the specified request.
    /// </summary>
    /// <param name="request">Request to validate</param>
    /// <returns>True if workflow can handle request</returns>
    bool CanHandle<T>(T request) where T : class;
}

/// <summary>
/// Types of analysis workflows supported.
/// </summary>
public enum AnalysisType
{
    /// <summary>
    /// Standard single measurement correlation analysis.
    /// </summary>
    SingleMeasurement,
    
    /// <summary>
    /// Multi-measurement analysis combining multiple indicators.
    /// </summary>
    MultiMeasurement,
    
    /// <summary>
    /// Contextual analysis examining context variable effects.
    /// </summary>
    Contextual,
    
    /// <summary>
    /// Real-time analysis for live trading systems.
    /// </summary>
    RealTime,
    
    /// <summary>
    /// Batch analysis for historical backtesting.
    /// </summary>
    Batch,
    
    /// <summary>
    /// Comparative analysis between different time periods.
    /// </summary>
    Comparative
}