using System;
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
using MedjCap.Data.TimeSeries.Commands;


namespace MedjCap.Data.Infrastructure.CQRS.Commands.Handlers;

/// <summary>
/// Command handler for adding a single data point.
/// </summary>
public class AddDataPointCommandHandler : ICommandHandler<AddDataPointCommand>
{
    private readonly IDataCollector _dataCollector;
    private readonly ILogger<AddDataPointCommandHandler> _logger;

    public AddDataPointCommandHandler(IDataCollector dataCollector, ILogger<AddDataPointCommandHandler> logger)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(AddDataPointCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        try
        {
            if (command.ContextualData != null)
            {
                _dataCollector.AddDataPoint(
                    command.Timestamp,
                    command.MeasurementId,
                    command.MeasurementValue,
                    command.Price,
                    command.ATR,
                    command.ContextualData);
            }
            else
            {
                _dataCollector.AddDataPoint(
                    command.Timestamp,
                    command.MeasurementId,
                    command.MeasurementValue,
                    command.Price,
                    command.ATR);
            }

            _logger.LogInformation("Added data point for measurement {MeasurementId} at {Timestamp}", 
                command.MeasurementId, command.Timestamp);

            await Task.CompletedTask; // Make method async for consistency
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add data point for measurement {MeasurementId} at {Timestamp}", 
                command.MeasurementId, command.Timestamp);
            throw;
        }
    }
}

/// <summary>
/// Command handler for adding multiple data points in a batch.
/// </summary>
public class AddMultipleDataPointCommandHandler : ICommandHandler<AddMultipleDataPointCommand>
{
    private readonly IDataCollector _dataCollector;
    private readonly ILogger<AddMultipleDataPointCommandHandler> _logger;

    public AddMultipleDataPointCommandHandler(IDataCollector dataCollector, ILogger<AddMultipleDataPointCommandHandler> logger)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(AddMultipleDataPointCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        try
        {
            if (command.ContextualData != null)
            {
                _dataCollector.AddMultipleDataPoint(
                    command.Timestamp,
                    command.Measurements,
                    command.Price,
                    command.ATR,
                    command.ContextualData);
            }
            else
            {
                _dataCollector.AddMultipleDataPoint(
                    command.Timestamp,
                    command.Measurements,
                    command.Price,
                    command.ATR);
            }

            _logger.LogInformation("Added {Count} measurements at {Timestamp}", 
                command.Measurements.Count, command.Timestamp);

            await Task.CompletedTask; // Make method async for consistency
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add {Count} measurements at {Timestamp}", 
                command.Measurements.Count, command.Timestamp);
            throw;
        }
    }
}

/// <summary>
/// Command handler for clearing all data points.
/// </summary>
public class ClearDataCommandHandler : ICommandHandler<ClearDataCommand>
{
    private readonly IDataCollector _dataCollector;
    private readonly ILogger<ClearDataCommandHandler> _logger;

    public ClearDataCommandHandler(IDataCollector dataCollector, ILogger<ClearDataCommandHandler> logger)
    {
        _dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ClearDataCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        try
        {
            var statsBeforeClear = _dataCollector.GetStatistics();
            _dataCollector.Clear();

            _logger.LogWarning("Cleared all data points (was {Count} points). Reason: {Reason}, Initiated by: {InitiatedBy}", 
                statsBeforeClear.TotalDataPoints, command.Reason, command.InitiatedBy);

            await Task.CompletedTask; // Make method async for consistency
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear data points. Reason: {Reason}", command.Reason);
            throw;
        }
    }
}