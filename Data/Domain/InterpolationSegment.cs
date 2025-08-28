namespace MedjCap.Data.Domain;

public record InterpolationSegment
{
    public decimal StartValue { get; init; }
    public decimal EndValue { get; init; }
    public double StartBias { get; init; }
    public double EndBias { get; init; }
    public string SegmentType { get; init; } = string.Empty;
    public decimal MeasurementRangeLow { get; init; }
    public decimal MeasurementRangeHigh { get; init; }
    public double BiasScoreLow { get; init; }
    public double BiasScoreHigh { get; init; }
    public string Description { get; init; } = string.Empty;
}