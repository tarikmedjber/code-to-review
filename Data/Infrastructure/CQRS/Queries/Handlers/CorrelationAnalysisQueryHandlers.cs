using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedjCap.Data.Infrastructure.Interfaces;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.DataQuality.Interfaces;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.TimeSeries.Interfaces;


using MedjCap.Data.Statistics.Correlation.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.TimeSeries.Models;

namespace MedjCap.Data.Infrastructure.CQRS.Queries.Handlers;

/// <summary>
/// Query handler for calculating price movements for a single time horizon.
/// </summary>
public class CalculatePriceMovementsQueryHandler : IQueryHandler<CalculatePriceMovementsQuery, List<PriceMovement>>
{
    private readonly ICorrelationService _correlationService;
    private readonly ILogger<CalculatePriceMovementsQueryHandler> _logger;

    public CalculatePriceMovementsQueryHandler(ICorrelationService correlationService, ILogger<CalculatePriceMovementsQueryHandler> logger)
    {
        _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<PriceMovement>> HandleAsync(CalculatePriceMovementsQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _correlationService.CalculatePriceMovements(query.TimeSeries, query.TimeHorizon);
            _logger.LogDebug("Calculated price movements for {TimeHorizon} via query with ID {QueryId}", 
                query.TimeHorizon, query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate price movements via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for calculating price movements for multiple time horizons.
/// </summary>
public class CalculateMultipleTimeHorizonPriceMovementsQueryHandler : IQueryHandler<CalculateMultipleTimeHorizonPriceMovementsQuery, Dictionary<TimeSpan, List<PriceMovement>>>
{
    private readonly ICorrelationService _correlationService;
    private readonly ILogger<CalculateMultipleTimeHorizonPriceMovementsQueryHandler> _logger;

    public CalculateMultipleTimeHorizonPriceMovementsQueryHandler(ICorrelationService correlationService, ILogger<CalculateMultipleTimeHorizonPriceMovementsQueryHandler> logger)
    {
        _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Dictionary<TimeSpan, List<PriceMovement>>> HandleAsync(CalculateMultipleTimeHorizonPriceMovementsQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _correlationService.CalculatePriceMovements(query.TimeSeries, query.TimeHorizons);
            _logger.LogDebug("Calculated price movements for {Count} time horizons via query with ID {QueryId}", 
                query.TimeHorizons.Length, query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate price movements for multiple horizons via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for calculating correlation.
/// </summary>
public class CalculateCorrelationQueryHandler : IQueryHandler<CalculateCorrelationQuery, CorrelationResult>
{
    private readonly ICorrelationService _correlationService;
    private readonly ILogger<CalculateCorrelationQueryHandler> _logger;

    public CalculateCorrelationQueryHandler(ICorrelationService correlationService, ILogger<CalculateCorrelationQueryHandler> logger)
    {
        _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CorrelationResult> HandleAsync(CalculateCorrelationQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _correlationService.CalculateCorrelation(query.Movements, query.CorrelationType);
            _logger.LogDebug("Calculated {CorrelationType} correlation for {Count} movements via query with ID {QueryId}", 
                query.CorrelationType, query.Movements.Count, query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate correlation via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for bucketizing movements.
/// </summary>
public class BucketizeMovementsQueryHandler : IQueryHandler<BucketizeMovementsQuery, Dictionary<string, List<PriceMovement>>>
{
    private readonly ICorrelationService _correlationService;
    private readonly ILogger<BucketizeMovementsQueryHandler> _logger;

    public BucketizeMovementsQueryHandler(ICorrelationService correlationService, ILogger<BucketizeMovementsQueryHandler> logger)
    {
        _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Dictionary<string, List<PriceMovement>>> HandleAsync(BucketizeMovementsQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _correlationService.BucketizeMovements(query.Movements, query.ATRTargets);
            _logger.LogDebug("Bucketized {Count} movements into {Buckets} buckets via query with ID {QueryId}", 
                query.Movements.Count, query.ATRTargets.Length, query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bucketize movements via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for analyzing by measurement ranges.
/// </summary>
public class AnalyzeByMeasurementRangesQueryHandler : IQueryHandler<AnalyzeByMeasurementRangesQuery, Dictionary<string, RangeAnalysisResult>>
{
    private readonly ICorrelationService _correlationService;
    private readonly ILogger<AnalyzeByMeasurementRangesQueryHandler> _logger;

    public AnalyzeByMeasurementRangesQueryHandler(ICorrelationService correlationService, ILogger<AnalyzeByMeasurementRangesQueryHandler> logger)
    {
        _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Dictionary<string, RangeAnalysisResult>> HandleAsync(AnalyzeByMeasurementRangesQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _correlationService.AnalyzeByMeasurementRanges(query.Movements, query.MeasurementRanges);
            _logger.LogDebug("Analyzed {Count} movements across {Ranges} ranges via query with ID {QueryId}", 
                query.Movements.Count, query.MeasurementRanges.Count, query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze by measurement ranges via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for calculating correlation with contextual filter.
/// </summary>
public class CalculateCorrelationWithContextualFilterQueryHandler : IQueryHandler<CalculateCorrelationWithContextualFilterQuery, CorrelationResult>
{
    private readonly ICorrelationService _correlationService;
    private readonly ILogger<CalculateCorrelationWithContextualFilterQueryHandler> _logger;

    public CalculateCorrelationWithContextualFilterQueryHandler(ICorrelationService correlationService, ILogger<CalculateCorrelationWithContextualFilterQueryHandler> logger)
    {
        _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CorrelationResult> HandleAsync(CalculateCorrelationWithContextualFilterQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _correlationService.CalculateWithContextualFilter(
                query.Movements, 
                query.ContextVariable, 
                query.ContextThreshold, 
                query.ComparisonOperator);
            
            _logger.LogDebug("Calculated contextual correlation for variable {Variable} via query with ID {QueryId}", 
                query.ContextVariable, query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate contextual correlation via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}

/// <summary>
/// Query handler for running full correlation analysis.
/// </summary>
public class RunFullCorrelationAnalysisQueryHandler : IQueryHandler<RunFullCorrelationAnalysisQuery, CorrelationAnalysisResult>
{
    private readonly ICorrelationService _correlationService;
    private readonly ILogger<RunFullCorrelationAnalysisQueryHandler> _logger;

    public RunFullCorrelationAnalysisQueryHandler(ICorrelationService correlationService, ILogger<RunFullCorrelationAnalysisQueryHandler> logger)
    {
        _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CorrelationAnalysisResult> HandleAsync(RunFullCorrelationAnalysisQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var result = _correlationService.RunFullAnalysis(query.TimeSeries, query.Request);
            _logger.LogDebug("Completed full correlation analysis for measurement {MeasurementId} via query with ID {QueryId}", 
                query.Request.MeasurementId, query.QueryId);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run full correlation analysis via query with ID {QueryId}", query.QueryId);
            throw;
        }
    }
}