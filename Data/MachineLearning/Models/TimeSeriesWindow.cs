using MedjCap.Data.Trading.Models;
using MedjCap.Data.Infrastructure.Models;

namespace MedjCap.Data.MachineLearning.Models;

/// <summary>
/// Helper class for time-series validation windows.
/// Contains training and validation data for cross-validation strategies.
/// </summary>
public class TimeSeriesWindow
{
    public List<PriceMovement> TrainingData { get; set; } = new();
    public List<PriceMovement> ValidationData { get; set; } = new();
    public DateRange TrainingPeriod { get; set; } = new();
    public DateRange ValidationPeriod { get; set; } = new();
}