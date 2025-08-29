using MedjCap.Data.Infrastructure.Interfaces;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.DataQuality.Interfaces;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.Analysis.Models;

namespace MedjCap.Data.Analysis.Storage;

/// <summary>
/// Repository implementation using abstracted storage for analysis results and configurations
/// </summary>
public class AnalysisRepository : IAnalysisRepository
{
    private readonly IAnalysisStorage _analysisStorage;
    private readonly IConfigurationStorage _configurationStorage;
    
    public AnalysisRepository(IAnalysisStorage analysisStorage, IConfigurationStorage configurationStorage)
    {
        _analysisStorage = analysisStorage ?? throw new ArgumentNullException(nameof(analysisStorage));
        _configurationStorage = configurationStorage ?? throw new ArgumentNullException(nameof(configurationStorage));
    }
    public async Task SaveAnalysisResult(AnalysisResult result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        await _analysisStorage.SaveAsync(result);
    }

    public async Task<AnalysisResult?> GetAnalysisResult(string analysisId)
    {
        if (string.IsNullOrEmpty(analysisId)) throw new ArgumentNullException(nameof(analysisId));
        return await _analysisStorage.GetByIdAsync(analysisId);
    }

    public async Task<List<AnalysisResult>> GetAnalysisHistory(string measurementId, DateTime? from = null, DateTime? to = null)
    {
        if (string.IsNullOrEmpty(measurementId)) throw new ArgumentNullException(nameof(measurementId));
        
        var results = await _analysisStorage.GetHistoryAsync(measurementId, from, to);
        return results.ToList();
    }

    public async Task SaveConfiguration(AnalysisConfig config, string configurationName)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrEmpty(configurationName)) throw new ArgumentNullException(nameof(configurationName));
        
        await _configurationStorage.SaveConfigurationAsync(config, configurationName);
    }

    public async Task<AnalysisConfig?> GetConfiguration(string configurationName)
    {
        if (string.IsNullOrEmpty(configurationName)) throw new ArgumentNullException(nameof(configurationName));
        return await _configurationStorage.GetConfigurationAsync(configurationName);
    }
}