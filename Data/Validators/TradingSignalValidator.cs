using MedjCap.Data.Extensions;

namespace MedjCap.Data.Validators;

public static class TradingSignalValidator
{
    public static bool IsBullishSignal(decimal measurementValue, decimal price, decimal atr)
    {
        return measurementValue.IsInBullishRange() && 
               price.IsValidPrice() && 
               atr.IsValidAtr();
    }

    public static bool IsBearishSignal(decimal measurementValue, decimal price, decimal atr)
    {
        return measurementValue.IsInBearishRange() && 
               price.IsValidPrice() && 
               atr.IsValidAtr();
    }

    public static bool RequiresOptimization(double correlation, int sampleSize, double degradation)
    {
        return !correlation.MeetsCorrelationThreshold() || 
               !sampleSize.IsAdequateSampleSize() || 
               !degradation.IsAcceptableDegradation();
    }

    public static bool IsValidTradingCondition(decimal measurementValue, decimal price, decimal atr, double correlation, int sampleSize)
    {
        return (measurementValue.IsInBullishRange() || measurementValue.IsInBearishRange()) &&
               price.IsValidPrice() &&
               atr.IsValidAtr() &&
               correlation.IsStrongCorrelation() &&
               sampleSize.IsAdequateSampleSize();
    }

    public static bool HasSignificantCorrelation(double coefficient, double pValue, double threshold = 0.3)
    {
        return coefficient.IsStrongCorrelation(threshold) && 
               pValue.IsStatisticallySignificant();
    }

    public static bool IsValidAnalysisInput(int sampleSize, double variance = 0.01)
    {
        return sampleSize.IsAdequateSampleSize(30) && // Lower threshold for basic analysis
               variance > 0; // Basic variance check
    }

    public static bool ShouldTriggerAlert(double correlation, double threshold, double pValue = 0.05)
    {
        return correlation.MeetsCorrelationThreshold(threshold) && 
               pValue.IsStatisticallySignificant();
    }

    public static bool IsOutOfBounds(decimal value, decimal lowerBound, decimal upperBound)
    {
        return !value.IsWithinRange(lowerBound, upperBound);
    }

    public static bool RequiresRebalancing(double performanceDegradation, int daysSinceLastRebalance, int maxDaysThreshold = 30)
    {
        return !performanceDegradation.IsAcceptableDegradation() || 
               daysSinceLastRebalance >= maxDaysThreshold;
    }
}