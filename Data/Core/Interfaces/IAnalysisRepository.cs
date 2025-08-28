using MedjCap.Data.Domain;

namespace MedjCap.Data.Core;

/// <summary>
/// Repository for persisting and retrieving analysis results and configurations.
/// Provides caching and historical analysis tracking capabilities.
/// </summary>
public interface IAnalysisRepository
{
    Task SaveAnalysisResult(AnalysisResult result);
    Task<AnalysisResult?> GetAnalysisResult(string analysisId);
    Task<List<AnalysisResult>> GetAnalysisHistory(string measurementId, DateTime? from = null, DateTime? to = null);
    Task SaveConfiguration(AnalysisConfig config, string configurationName);
    Task<AnalysisConfig?> GetConfiguration(string configurationName);
}