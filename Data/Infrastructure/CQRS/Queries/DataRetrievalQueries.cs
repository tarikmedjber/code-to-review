using System;
using System.Collections.Generic;

using MedjCap.Data.Statistics.Correlation.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.Statistics.Models;

namespace MedjCap.Data.Infrastructure.CQRS.Queries;

/// <summary>
/// Query to retrieve all data points.
/// </summary>
public sealed record GetDataPointsQuery : BaseQuery<IEnumerable<DataPoint>>
{
}

/// <summary>
/// Query to retrieve all multi-measurement data points.
/// </summary>
public sealed record GetMultiDataPointsQuery : BaseQuery<IEnumerable<MultiDataPoint>>
{
}

/// <summary>
/// Query to retrieve data points for a specific measurement type.
/// </summary>
public sealed record GetDataByMeasurementIdQuery : BaseQuery<IEnumerable<DataPoint>>
{
    /// <summary>
    /// The measurement identifier to filter by.
    /// </summary>
    public string MeasurementId { get; init; } = string.Empty;
}

/// <summary>
/// Query to retrieve data points within a specific date range.
/// </summary>
public sealed record GetDataByDateRangeQuery : BaseQuery<IEnumerable<DataPoint>>
{
    /// <summary>
    /// The date range to filter data points.
    /// </summary>
    public DateRange DateRange { get; init; } = new();
}

/// <summary>
/// Query to get time series data for analysis.
/// </summary>
public sealed record GetTimeSeriesDataQuery : BaseQuery<TimeSeriesData>
{
}

/// <summary>
/// Query to get statistical summary of collected data.
/// </summary>
public sealed record GetDataStatisticsQuery : BaseQuery<DataStatistics>
{
}