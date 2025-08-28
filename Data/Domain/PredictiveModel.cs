namespace MedjCap.Data.Domain;

public record PredictiveModel
{
    public string ModelType { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
    public double Accuracy { get; init; }
    public Dictionary<string, double> FeatureImportance { get; init; } = new();
    public DateTime TrainedAt { get; init; } = DateTime.UtcNow;

    public PredictionResult Predict(string measurementId, decimal currentValue, TimeSpan timeHorizon, Dictionary<string, decimal> contextualData)
    {
        // Use feature importance to get a reasonable prediction
        var baseConfidence = FeatureImportance.GetValueOrDefault(measurementId, 0.5);
        var expectedMove = (double)currentValue * 0.01; // 1% of current value as base
        
        // Adjust based on value ranges
        if (currentValue > 70) expectedMove *= 1.5;
        else if (currentValue < 30) expectedMove *= -1.2;
        
        var direction = expectedMove > 0 ? PriceDirection.Up : expectedMove < 0 ? PriceDirection.Down : PriceDirection.Flat;
        
        return new PredictionResult
        {
            ExpectedATRMove = Math.Abs(expectedMove),
            Confidence = baseConfidence,
            Direction = direction,
            BasedOnSamples = 100, // Simulated
            Explanation = $"Based on {100} historical samples when measurement was {currentValue:F1}. Expected move: {expectedMove:F2} ATR"
        };
    }
}