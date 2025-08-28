namespace MedjCap.Data.Domain;

public record LiveExpectation
{
    public string MeasurementId { get; init; } = string.Empty;
    public decimal CurrentValue { get; init; }
    public decimal CurrentMeasurementValue { get; init; }
    public double ExpectedBias { get; init; }
    public double Confidence { get; init; }
    public TimeSpan TimeHorizon { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public double ExpectedATRMove { get; init; }
    public string Signal { get; init; } = string.Empty;
    public string Rationale { get; init; } = string.Empty;
    public int SimilarHistoricalSamples { get; init; }
}