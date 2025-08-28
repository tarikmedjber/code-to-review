namespace MedjCap.Data.Domain;

public record PredictionResult
{
    public double ExpectedATRMove { get; init; }
    public double Confidence { get; init; }
    public PriceDirection Direction { get; init; }
    public int BasedOnSamples { get; init; }
    public string Explanation { get; init; } = string.Empty;
}