using System;
using System.Collections.Generic;
using MedjCap.Data.Infrastructure.CQRS;


namespace MedjCap.Data.TimeSeries.Commands;

/// <summary>
/// Command to add a single data point to the data collection.
/// </summary>
public sealed record AddDataPointCommand : BaseCommand
{
    /// <summary>
    /// Time of the measurement.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Unique identifier for the measurement type.
    /// </summary>
    public string MeasurementId { get; init; } = string.Empty;

    /// <summary>
    /// The measurement value.
    /// </summary>
    public decimal MeasurementValue { get; init; }

    /// <summary>
    /// Market price at the time of measurement.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Average True Range at the time of measurement.
    /// </summary>
    public decimal ATR { get; init; }

    /// <summary>
    /// Additional market context (optional).
    /// </summary>
    public Dictionary<string, decimal>? ContextualData { get; init; }
}

/// <summary>
/// Command to add multiple data points in a batch operation.
/// </summary>
public sealed record AddMultipleDataPointCommand : BaseCommand
{
    /// <summary>
    /// Time of the measurements.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Dictionary of measurement ID to value pairs.
    /// </summary>
    public Dictionary<string, decimal> Measurements { get; init; } = new();

    /// <summary>
    /// Market price at the time of measurements.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Average True Range at the time of measurements.
    /// </summary>
    public decimal ATR { get; init; }

    /// <summary>
    /// Additional market context (optional).
    /// </summary>
    public Dictionary<string, decimal>? ContextualData { get; init; }
}

/// <summary>
/// Command to clear all collected data points.
/// </summary>
public sealed record ClearDataCommand : BaseCommand
{
    /// <summary>
    /// Reason for clearing the data (for audit purposes).
    /// </summary>
    public string Reason { get; init; } = "Manual clear operation";

    /// <summary>
    /// User or system that initiated the clear operation.
    /// </summary>
    public string InitiatedBy { get; init; } = "System";
}