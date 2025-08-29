namespace MedjCap.Data.Infrastructure.Models;

/// <summary>
/// Defines comparison operators for contextual filtering.
/// Used to filter data based on contextual variable thresholds.
/// </summary>
public enum ComparisonOperator
{
    GreaterThan,
    LessThan,
    Equal,
    GreaterThanOrEqual,
    LessThanOrEqual
}