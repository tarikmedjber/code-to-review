namespace MedjCap.Data.Statistics.Models;

public record ContextGroup
{
    public string GroupName { get; init; } = string.Empty;
    public decimal MinValue { get; init; }
    public decimal MaxValue { get; init; }
    public double AverageCorrelation { get; init; }
    public int SampleCount { get; init; }
    public Dictionary<TimeSpan, double> TimeHorizonCorrelations { get; init; } = new();
    public string ContextRange { get; init; } = string.Empty;
    public double Correlation { get; init; }
    public double AverageATRMove { get; init; }
    public double ProbabilityUp { get; init; }
}