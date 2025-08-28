namespace MedjCap.Data.Domain;

/// <summary>
/// Defines the type of correlation coefficient to calculate.
/// Different types handle different data distributions and relationships.
/// </summary>
public enum CorrelationType
{
    /// <summary>
    /// Pearson product-moment correlation - measures linear relationships.
    /// </summary>
    Pearson,

    /// <summary>
    /// Spearman rank correlation - measures monotonic relationships.
    /// </summary>
    Spearman,

    /// <summary>
    /// Kendall tau correlation - measures ordinal associations.
    /// </summary>
    KendallTau
}