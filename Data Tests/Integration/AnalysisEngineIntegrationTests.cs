using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.Analysis.Services;
using MedjCap.Data.Analysis.Storage;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.Statistics.Services;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.MachineLearning.Services;
using MedjCap.Data.MachineLearning.Services.OptimizationStrategies;
using MedjCap.Data.MachineLearning.Optimization.Models;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.Backtesting.Services;
using MedjCap.Data.Backtesting.Models;
using MedjCap.Data.Infrastructure.Interfaces;
using MedjCap.Data.Infrastructure.Storage;
using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.TimeSeries.Services;
using MedjCap.Data.TimeSeries.Storage.InMemory;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.Tests.Helpers;

namespace MedjCap.Data.Tests.Integration
{
    public class AnalysisEngineIntegrationTests
    {
        private readonly IAnalysisEngine _analysisEngine;

        public AnalysisEngineIntegrationTests()
        {
            // Create storage implementations
            var timeSeriesStorage = new InMemoryTimeSeriesDataStorage();
            var multiDataStorage = new InMemoryMultiDataStorage();
            var analysisStorage = new InMemoryAnalysisStorage();
            var configurationStorage = new InMemoryConfigurationStorage();
            
            var strategyFactory = new OptimizationStrategyFactory(TestConfigurationHelper.CreateDefaultOptimizationConfig());
            
            _analysisEngine = new AnalysisEngine(
                new DataCollector(timeSeriesStorage, multiDataStorage),
                new CorrelationService(TestConfigurationHelper.CreateDefaultStatisticalConfig()),
                new MLBoundaryOptimizer(TestConfigurationHelper.CreateDefaultOptimizationConfig(), strategyFactory),
                new BacktestService(
                    TestConfigurationHelper.CreateDefaultValidationConfig(),
                    TestConfigurationHelper.CreateDefaultStatisticalConfig()),
                new AnalysisRepository(analysisStorage, configurationStorage)
            );
        }

        [Fact]
        public async Task RunAnalysis_GivenCompleteWorkflow_WhenExecuted_ThenReturnsFullResults()
        {
            // Given - Complete analysis configuration
            var config = new AnalysisConfig
            {
                InSample = new DateRange
                {
                    Start = new DateTime(2024, 1, 1, 9, 0, 0),
                    End = new DateTime(2024, 1, 1, 11, 0, 0)
                },
                OutOfSamples = new List<DateRange>
                {
                    new DateRange
                    {
                        Start = new DateTime(2024, 1, 1, 11, 0, 0),
                        End = new DateTime(2024, 1, 1, 12, 0, 0)
                    }
                },
                WalkForwardWindows = 3,
                MinSampleSize = 20,
                ConfidenceLevel = 0.95
            };

            // Load historical data
            await LoadSampleData();

            var request = new AnalysisRequest
            {
                MeasurementId = "PriceVa_Score",
                TimeHorizons = new[] { TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromHours(1) },
                ATRTargets = new[] { 0.5m, 1.0m, 1.5m, 2.0m },
                OptimizationTarget = OptimizationTarget.RiskAdjustedReturn,
                Config = config
            };

            // When
            var result = await _analysisEngine.RunAnalysisAsync(request);

            // Then
            result.Should().NotBeNull();
            result.Status.Should().Be(AnalysisStatus.Completed);
            result.MeasurementId.Should().Be("PriceVa_Score");
            
            // Should have correlations for each time horizon
            result.CorrelationResults.Should().HaveCount(3);
            result.CorrelationResults.Should().ContainKey(TimeSpan.FromMinutes(15));
            
            // Should have ML-optimized boundaries
            result.OptimalBoundaries.Should().NotBeEmpty();
            result.OptimalBoundaries.First().Confidence.Should().BeGreaterThan(0);
            
            // Should have walk-forward validation results
            result.WalkForwardResults.Should().NotBeNull();
            result.WalkForwardResults.WindowCount.Should().Be(3);
            result.WalkForwardResults.IsStable.Should().BeTrue();
        }

        [Fact]
        public async Task RunAnalysisWithWalkForward_GivenDateRanges_WhenExecuted_ThenPerformsValidation()
        {
            // Given
            var config = new AnalysisConfig
            {
                InSample = new DateRange
                {
                    Start = new DateTime(2024, 1, 1, 9, 0, 0),
                    End = new DateTime(2024, 1, 1, 15, 0, 0) // 6 hours
                },
                OutOfSamples = new List<DateRange>
                {
                    new DateRange
                    {
                        Start = new DateTime(2024, 1, 1, 15, 0, 0),
                        End = new DateTime(2024, 1, 1, 17, 0, 0) // 2 hours
                    },
                    new DateRange
                    {
                        Start = new DateTime(2024, 1, 2, 9, 0, 0),
                        End = new DateTime(2024, 1, 2, 11, 0, 0) // Another 2 hours
                    }
                },
                WalkForwardWindows = 5,
                MinSampleSize = 10,
                ConfidenceLevel = 0.95
            };

            await LoadSampleData();

            var request = new AnalysisRequest
            {
                MeasurementId = "PriceVa_Score",
                TimeHorizons = new[] { TimeSpan.FromMinutes(30) },
                ATRTargets = new[] { 1.0m, 2.0m },
                Config = config
            };

            // When
            var result = await _analysisEngine.RunAnalysisAsync(request);

            // Then - Walk-forward validation should split the in-sample period
            result.WalkForwardResults.Should().NotBeNull();
            result.WalkForwardResults.WindowCount.Should().Be(5);
            
            foreach (var window in result.WalkForwardResults.Windows)
            {
                window.InSamplePeriod.Should().NotBeNull();
                window.OutOfSamplePeriod.Should().NotBeNull();
                window.InSampleCorrelation.Should().NotBe(0);
                window.OutOfSampleCorrelation.Should().NotBe(0);
                window.PerformanceDegradation.Should().BeLessThan(1.0); // Some degradation is normal
            }
            
            // Overall stability assessment
            result.WalkForwardResults.AverageCorrelation.Should().BeGreaterThan(0);
            result.WalkForwardResults.CorrelationStdDev.Should().BeLessThan(0.5);
        }

        [Fact]
        public async Task GenerateTableOutput_GivenAnalysisResults_WhenRequested_ThenReturnsFormattedTable()
        {
            // Given
            await LoadSampleData();
            var request = CreateBasicAnalysisRequest();
            var analysisResult = await _analysisEngine.RunAnalysisAsync(request);

            // When
            var tableOutput = analysisResult.GetTableFormat();

            // Then
            tableOutput.Should().NotBeNull();
            tableOutput.Headers.Should().Contain("Measurement Range");
            tableOutput.Headers.Should().Contain("Probability Up");
            tableOutput.Headers.Should().Contain("Avg ATR Move");
            tableOutput.Headers.Should().Contain("Sample Count");
            tableOutput.Headers.Should().Contain("Confidence");
            
            tableOutput.Rows.Should().NotBeEmpty();
            
            var firstRow = tableOutput.Rows.First();
            firstRow.Should().ContainKey("MeasurementRange");
            firstRow.Should().ContainKey("ProbabilityUp");
            firstRow.Should().ContainKey("AvgATRMove");
            firstRow["SampleCount"].Should().BeOfType<int>();
            
            // Table should be sorted by confidence
            var confidences = tableOutput.Rows.Select(r => (double)r["Confidence"]).ToList();
            confidences.Should().BeInDescendingOrder();
        }

        [Fact]
        public async Task GeneratePredictiveModel_GivenAnalysis_WhenRequested_ThenReturnsPredictions()
        {
            // Given
            await LoadSampleData();
            var request = CreateBasicAnalysisRequest();
            var analysisResult = await _analysisEngine.RunAnalysisAsync(request);

            // When
            var predictiveModel = analysisResult.GetPredictiveModel();
            
            var prediction = predictiveModel.Predict(
                measurementId: "PriceVa_Score",
                currentValue: 65m,
                timeHorizon: TimeSpan.FromMinutes(30),
                contextualData: new Dictionary<string, decimal>
                {
                    { "Daily_Score", 70m }
                }
            );

            // Then
            prediction.Should().NotBeNull();
            prediction.ExpectedATRMove.Should().NotBe(0);
            prediction.Confidence.Should().BeInRange(0, 1);
            prediction.Direction.Should().BeOneOf(PriceDirection.Up, PriceDirection.Down, PriceDirection.Flat);
            prediction.BasedOnSamples.Should().BeGreaterThan(0);
            prediction.Explanation.Should().NotBeNullOrEmpty();
            
            // Should explain the prediction
            prediction.Explanation.Should().Contain("65");
            prediction.Explanation.Should().Contain("samples");
        }

        [Fact]
        public async Task GenerateStatisticalReport_GivenAnalysis_WhenRequested_ThenReturnsStatistics()
        {
            // Given
            await LoadSampleData();
            var request = CreateBasicAnalysisRequest();
            var analysisResult = await _analysisEngine.RunAnalysisAsync(request);

            // When
            var statistics = analysisResult.GetStatisticalReport();

            // Then
            statistics.Should().NotBeNull();
            
            // Correlation statistics
            statistics.Correlations.Should().NotBeEmpty();
            foreach (var correlation in statistics.Correlations)
            {
                correlation.TimeHorizon.Should().BeGreaterThan(TimeSpan.Zero);
                correlation.PearsonCoefficient.Should().BeInRange(-1, 1);
                correlation.SpearmanCoefficient.Should().BeInRange(-1, 1);
                correlation.PValue.Should().BeInRange(0, 1);
                correlation.IsSignificant.Should().Be(correlation.PValue < 0.05);
            }
            
            // Overall statistics
            statistics.TotalSamples.Should().BeGreaterThan(0);
            statistics.DateRangeAnalyzed.Should().NotBeNull();
            statistics.OptimalMeasurementRange.Should().NotBeNull();
            statistics.BestTimeHorizon.Should().BeGreaterThan(TimeSpan.Zero);
            statistics.MaxCorrelation.Should().BeInRange(-1, 1);
        }

        [Fact]
        public async Task GenerateInterpolationSegments_GivenAnalysis_WhenRequested_ThenReturnsSegments()
        {
            // Given
            await LoadSampleData();
            var request = CreateBasicAnalysisRequest();
            var analysisResult = await _analysisEngine.RunAnalysisAsync(request);

            // When
            var segments = analysisResult.ToInterpolationSegments();

            // Then
            segments.Should().NotBeEmpty();
            
            foreach (var segment in segments)
            {
                segment.MeasurementRangeLow.Should().BeLessThan(segment.MeasurementRangeHigh);
                segment.BiasScoreLow.Should().BeInRange(-100, 100);
                segment.BiasScoreHigh.Should().BeInRange(-100, 100);
                segment.Description.Should().NotBeNullOrEmpty();
                
                // Description should explain the behavior
                segment.Description.Should().ContainAny("Bullish", "Bearish", "Neutral", "Strong", "Weak");
            }
            
            // Segments should cover the full range of observed values
            var minObserved = segments.Min(s => s.MeasurementRangeLow);
            var maxObserved = segments.Max(s => s.MeasurementRangeHigh);
            minObserved.Should().BeLessThanOrEqualTo(40);
            maxObserved.Should().BeGreaterThanOrEqualTo(80);
        }

        [Fact]
        public async Task RunAnalysisWithMultipleMeasurements_GivenCombination_WhenAnalyzed_ThenFindsOptimalWeights()
        {
            // Given
            await LoadMultiMeasurementData();
            
            var request = new MultiMeasurementAnalysisRequest
            {
                MeasurementIds = new[] { "PriceVa_Score", "FastVahaVaAtr_Score", "PriceVa_Change" },
                TimeHorizons = new[] { TimeSpan.FromMinutes(30) },
                ATRTargets = new[] { 1.0m, 2.0m },
                OptimizationTarget = OptimizationTarget.RiskAdjustedReturn,
                Config = new AnalysisConfig
                {
                    InSample = new DateRange
                    {
                        Start = new DateTime(2024, 1, 1, 9, 0, 0),
                        End = new DateTime(2024, 1, 1, 12, 0, 0)
                    },
                    OutOfSamples = new List<DateRange>(),
                    WalkForwardWindows = 1,
                    MinSampleSize = 10
                }
            };

            // When
            var result = await _analysisEngine.RunMultiMeasurementAnalysisAsync(request);

            // Then
            result.Should().NotBeNull();
            result.OptimalWeights.Should().HaveCount(3);
            result.OptimalWeights.Values.Sum().Should().BeApproximately(1.0, 0.01); // Weights sum to 1
            
            // Should identify which measurement is most predictive
            result.MeasurementImportance.Should().NotBeEmpty();
            var mostImportant = result.MeasurementImportance.OrderByDescending(m => m.Value).First();
            mostImportant.Value.Should().BeGreaterThan(0.3); // At least 30% importance
            
            // Combined model should perform better than individual
            result.CombinedCorrelation.Should().BeGreaterThan(0);
            result.IndividualCorrelations.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetLiveExpectation_GivenCurrentMarketConditions_WhenQueried_ThenReturnsExpectation()
        {
            // Given
            await LoadSampleData();
            var request = CreateBasicAnalysisRequest();
            var analysisResult = await _analysisEngine.RunAnalysisAsync(request);

            // When
            var liveExpectation = analysisResult.GetLiveExpectation(
                measurementId: "PriceVa_Score",
                currentValue: 68m,
                currentContext: new Dictionary<string, decimal>
                {
                    { "Volatility", 2m },
                    { "Daily_Score", 75m }
                }
            );

            // Then
            liveExpectation.Should().NotBeNull();
            liveExpectation.CurrentMeasurementValue.Should().Be(68m);
            liveExpectation.ExpectedATRMove.Should().NotBe(0);
            liveExpectation.Confidence.Should().BeInRange(0, 1);
            liveExpectation.TimeHorizon.Should().BeGreaterThan(TimeSpan.Zero);
            
            // Should provide actionable information
            liveExpectation.Signal.Should().BeOneOf("Strong Buy", "Buy", "Neutral", "Sell", "Strong Sell");
            liveExpectation.Rationale.Should().Contain("historical");
            liveExpectation.SimilarHistoricalSamples.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task AnalyzeContextualEffects_GivenContextVariable_WhenAnalyzed_ThenShowsImpact()
        {
            // Given
            await LoadSampleDataWithContext();
            
            var request = new ContextualAnalysisRequest
            {
                PrimaryMeasurement = "PriceVa_Score",
                ContextVariable = "Daily_Score",
                ContextThresholds = new[] { 40m, 60m, 80m },
                TimeHorizon = TimeSpan.FromMinutes(30),
                Config = CreateBasicConfig()
            };

            // When
            var result = await _analysisEngine.RunContextualAnalysisAsync(request);

            // Then
            result.Should().NotBeNull();
            result.ContextGroups.Should().HaveCount(4); // <40, 40-60, 60-80, >80
            
            foreach (var group in result.ContextGroups)
            {
                group.ContextRange.Should().NotBeNullOrEmpty();
                group.Correlation.Should().BeInRange(-1, 1);
                group.AverageATRMove.Should().NotBe(0);
                group.SampleCount.Should().BeGreaterThan(0);
                group.ProbabilityUp.Should().BeInRange(0, 1);
            }
            
            // Should show how correlation changes with context
            var lowContext = result.ContextGroups.First(g => g.ContextRange.Contains("<40"));
            var highContext = result.ContextGroups.First(g => g.ContextRange.Contains(">80"));
            
            // These should be different, showing context matters
            Math.Abs(lowContext.Correlation - highContext.Correlation).Should().BeGreaterThan(0.1);
        }

        // Helper methods
        private async Task LoadSampleData()
        {
            var dataCollector = await _analysisEngine.GetDataCollectorAsync();
            var baseTime = new DateTime(2024, 1, 1, 9, 0, 0);
            var random = new Random(42);
            
            // Generate 500 data points (8+ hours of 1-minute data)
            for (int i = 0; i < 500; i++)
            {
                var timestamp = baseTime.AddMinutes(i);
                var measurementValue = 40 + 30 * Math.Sin(i * 0.1) + random.Next(-10, 10);
                var price = 100m + i * 0.1m + (decimal)(random.NextDouble() - 0.5) * 5;
                var atr = 10m + (decimal)Math.Sin(i * 0.05);
                
                dataCollector.AddDataPoint(
                    timestamp,
                    "PriceVa_Score",
                    (decimal)measurementValue,
                    price,
                    atr,
                    new Dictionary<string, decimal>
                    {
                        { "Daily_Score", 60 + (decimal)(20 * Math.Sin(i * 0.01)) },
                        { "Volatility", random.Next(1, 4) }
                    }
                );
            }
        }

        private async Task LoadMultiMeasurementData()
        {
            var dataCollector = await _analysisEngine.GetDataCollectorAsync();
            var baseTime = new DateTime(2024, 1, 1, 9, 0, 0);
            var random = new Random(42);
            
            for (int i = 0; i < 200; i++)
            {
                var timestamp = baseTime.AddMinutes(i);
                
                dataCollector.AddMultipleDataPoint(
                    timestamp,
                    new Dictionary<string, decimal>
                    {
                        { "PriceVa_Score", 40 + 30 * (decimal)Math.Sin(i * 0.1) + random.Next(-10, 10) },
                        { "FastVahaVaAtr_Score", 50 + 20 * (decimal)Math.Cos(i * 0.15) + random.Next(-5, 5) },
                        { "PriceVa_Change", (decimal)(random.NextDouble() - 0.5) * 10 }
                    },
                    price: 100m + i * 0.1m + (decimal)(random.NextDouble() - 0.5) * 5,
                    atr: 10m
                );
            }
        }

        private async Task LoadSampleDataWithContext()
        {
            var dataCollector = await _analysisEngine.GetDataCollectorAsync();
            var baseTime = new DateTime(2024, 1, 1, 9, 0, 0);
            
            // Create data where context matters
            for (int i = 0; i < 200; i++)
            {
                var dailyScore = i < 50 ? 30m : i < 100 ? 50m : i < 150 ? 70m : 90m;
                var measurementValue = 50 + (decimal)(20 * Math.Sin(i * 0.1));
                
                // When daily score is high, positive correlation
                // When daily score is low, negative correlation
                var priceMove = dailyScore > 60 
                    ? measurementValue * 0.02m 
                    : -measurementValue * 0.01m;
                
                dataCollector.AddDataPoint(
                    baseTime.AddMinutes(i),
                    "PriceVa_Score",
                    measurementValue,
                    100m + priceMove,
                    10m,
                    new Dictionary<string, decimal> { { "Daily_Score", dailyScore } }
                );
            }
        }

        private AnalysisRequest CreateBasicAnalysisRequest()
        {
            return new AnalysisRequest
            {
                MeasurementId = "PriceVa_Score",
                TimeHorizons = new[] { TimeSpan.FromMinutes(30) },
                ATRTargets = new[] { 1.0m, 2.0m },
                OptimizationTarget = OptimizationTarget.RiskAdjustedReturn,
                Config = CreateBasicConfig()
            };
        }

        private AnalysisConfig CreateBasicConfig()
        {
            return new AnalysisConfig
            {
                InSample = new DateRange
                {
                    Start = new DateTime(2024, 1, 1, 9, 0, 0),
                    End = new DateTime(2024, 1, 1, 11, 0, 0)
                },
                OutOfSamples = new List<DateRange>(),
                WalkForwardWindows = 1,
                MinSampleSize = 10,
                ConfidenceLevel = 0.95
            };
        }
    }
}