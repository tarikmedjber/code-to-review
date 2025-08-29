using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using Moq;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Statistics.Services;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.Statistics.Models;
using MedjCap.Data.Statistics.Correlation.Models;
using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.Tests.Helpers;

namespace MedjCap.Data.Tests.Unit
{
    /// <summary>
    /// Isolated unit tests for CorrelationService using mocked dependencies
    /// Tests focus on pure correlation calculations without external dependencies
    /// </summary>
    public class CorrelationServiceTests
    {
        private readonly ICorrelationService _correlationService;

        public CorrelationServiceTests()
        {
            _correlationService = new CorrelationService(TestConfigurationHelper.CreateDefaultStatisticalConfig());
        }

        [Fact]
        public void CalculatePriceMovements_GivenTimeSeries_WhenCalculated_ThenReturnsATRMovements()
        {
            // Given - Time series data (directly created without dependencies)
            var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
            var dataPoints = new List<DataPoint>
            {
                new() { Timestamp = baseTime, MeasurementValue = 62m, Price = 100m, ATR = 10m },
                new() { Timestamp = baseTime.AddMinutes(30), MeasurementValue = 58m, Price = 125m, ATR = 10m },
                new() { Timestamp = baseTime.AddMinutes(60), MeasurementValue = 55m, Price = 150m, ATR = 10m }
            };

            var timeSeries = new TimeSeriesData { DataPoints = dataPoints };
            var timeHorizon = TimeSpan.FromMinutes(30);

            // When
            var movements = _correlationService.CalculatePriceMovements(timeSeries, timeHorizon);

            // Then
            movements.Should().HaveCount(2); // Only first 2 points can have 30-min forward movement
            
            // From T0: Price 100→125 = +25/10 = +2.5 ATR
            movements[0].StartTimestamp.Should().Be(baseTime);
            movements[0].MeasurementValue.Should().Be(62m);
            movements[0].ATRMovement.Should().Be(2.5m);
            movements[0].Direction.Should().Be(PriceDirection.Up);
            
            // From T+30: Price 125→150 = +25/10 = +2.5 ATR
            movements[1].StartTimestamp.Should().Be(baseTime.AddMinutes(30));
            movements[1].MeasurementValue.Should().Be(58m);
            movements[1].ATRMovement.Should().Be(2.5m);
        }

        [Fact]
        public void CalculatePriceMovements_GivenMultipleTimeHorizons_WhenCalculated_ThenReturnsAllMovements()
        {
            // Given - Mock data without external dependencies
            var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
            var dataPoints = new List<DataPoint>
            {
                new() { Timestamp = baseTime, MeasurementValue = 62m, Price = 100m, ATR = 10m },
                new() { Timestamp = baseTime.AddMinutes(30), MeasurementValue = 58m, Price = 125m, ATR = 10m },
                new() { Timestamp = baseTime.AddMinutes(60), MeasurementValue = 55m, Price = 150m, ATR = 10m },
                new() { Timestamp = baseTime.AddMinutes(90), MeasurementValue = 52m, Price = 140m, ATR = 10m }
            };

            var timeSeries = new TimeSeriesData { DataPoints = dataPoints };
            var timeHorizons = new[] { TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(60) };

            // When
            var movementsByHorizon = _correlationService.CalculatePriceMovements(timeSeries, timeHorizons);

            // Then
            movementsByHorizon.Should().HaveCount(2); // 2 time horizons
            
            // 30-minute movements
            movementsByHorizon[TimeSpan.FromMinutes(30)].Should().HaveCount(3);
            movementsByHorizon[TimeSpan.FromMinutes(30)][0].ATRMovement.Should().Be(2.5m); // 100→125
            
            // 60-minute movements  
            movementsByHorizon[TimeSpan.FromMinutes(60)].Should().HaveCount(2);
            movementsByHorizon[TimeSpan.FromMinutes(60)][0].ATRMovement.Should().Be(5.0m); // 100→150
        }

        [Fact]
        public void CalculateCorrelation_GivenMeasurementsAndMovements_WhenCalculated_ThenReturnsPearsonCoefficient()
        {
            // Given - Perfect positive correlation scenario
            var movements = new List<PriceMovement>
            {
                new() { MeasurementValue = 40m, ATRMovement = 0.5m },
                new() { MeasurementValue = 50m, ATRMovement = 1.0m },
                new() { MeasurementValue = 60m, ATRMovement = 1.5m },
                new() { MeasurementValue = 70m, ATRMovement = 2.0m },
                new() { MeasurementValue = 80m, ATRMovement = 2.5m }
            };

            // When
            var correlation = _correlationService.CalculateCorrelation(
                movements, 
                CorrelationType.Pearson);

            // Then
            correlation.Coefficient.Should().BeApproximately(1.0, 0.01); // Perfect positive correlation
            correlation.PValue.Should().BeLessThan(0.05); // Statistically significant
            correlation.SampleSize.Should().Be(5);
            correlation.IsStatisticallySignificant.Should().BeTrue();
        }

        [Fact]
        public void CalculateCorrelation_GivenNegativeRelationship_WhenCalculated_ThenReturnsNegativeCoefficient()
        {
            // Given - Negative correlation: higher measurements → lower movements
            var movements = new List<PriceMovement>
            {
                new() { MeasurementValue = 40m, ATRMovement = 2.0m },
                new() { MeasurementValue = 50m, ATRMovement = 1.5m },
                new() { MeasurementValue = 60m, ATRMovement = 1.0m },
                new() { MeasurementValue = 70m, ATRMovement = 0.5m },
                new() { MeasurementValue = 80m, ATRMovement = 0.0m }
            };

            // When
            var correlation = _correlationService.CalculateCorrelation(
                movements, 
                CorrelationType.Pearson);

            // Then
            correlation.Coefficient.Should().BeApproximately(-1.0, 0.01); // Perfect negative correlation
        }

        [Fact]
        public void BucketizeMovements_GivenATRTargets_WhenBucketized_ThenGroupsCorrectly()
        {
            // Given
            var movements = new List<PriceMovement>
            {
                new() { MeasurementValue = 60m, ATRMovement = 0.3m },  // Falls in 0-0.5 bucket
                new() { MeasurementValue = 62m, ATRMovement = 0.7m },  // Falls in 0.5-1.0 bucket
                new() { MeasurementValue = 65m, ATRMovement = 1.2m },  // Falls in 1.0-1.5 bucket
                new() { MeasurementValue = 68m, ATRMovement = 1.8m },  // Falls in 1.5-2.0 bucket
                new() { MeasurementValue = 70m, ATRMovement = 2.5m },  // Falls in 2.0+ bucket
            };
            
            var atrTargets = new[] { 0.5m, 1.0m, 1.5m, 2.0m };

            // When
            var buckets = _correlationService.BucketizeMovements(movements, atrTargets);

            // Then
            buckets.Should().HaveCount(5); // 4 ranges + 1 for above max
            buckets["0.0-0.5"].Should().HaveCount(1);
            buckets["0.5-1.0"].Should().HaveCount(1);
            buckets["1.0-1.5"].Should().HaveCount(1);
            buckets["1.5-2.0"].Should().HaveCount(1);
            buckets["2.0+"].Should().HaveCount(1);
        }

        [Fact]
        public void AnalyzeByMeasurementRanges_GivenData_WhenAnalyzed_ThenReturnsProbabilities()
        {
            // Given - Measurements in 60-70 range tend to go up
            var movements = new List<PriceMovement>
            {
                new() { MeasurementValue = 45m, ATRMovement = -1.0m }, // Down
                new() { MeasurementValue = 50m, ATRMovement = -0.5m }, // Down
                new() { MeasurementValue = 62m, ATRMovement = 1.5m },  // Up
                new() { MeasurementValue = 65m, ATRMovement = 2.0m },  // Up
                new() { MeasurementValue = 68m, ATRMovement = 1.8m },  // Up
                new() { MeasurementValue = 75m, ATRMovement = 0.5m },  // Up
                new() { MeasurementValue = 80m, ATRMovement = -0.3m }, // Down
            };

            var measurementRanges = new List<(decimal Low, decimal High)>
            {
                (40, 60),
                (60, 70),
                (70, 80)
            };

            // When
            var analysis = _correlationService.AnalyzeByMeasurementRanges(movements, measurementRanges);

            // Then
            analysis.Should().HaveCount(3);
            
            // 40-60 range: 2 samples, both down
            analysis["40-60"].ProbabilityUp.Should().Be(0.0);
            analysis["40-60"].AverageATRMove.Should().Be(-0.75m);
            analysis["40-60"].SampleCount.Should().Be(2);
            
            // 60-70 range: 3 samples, all up
            analysis["60-70"].ProbabilityUp.Should().Be(1.0);
            analysis["60-70"].AverageATRMove.Should().BeApproximately(1.77m, 0.01m);
            analysis["60-70"].SampleCount.Should().Be(3);
            
            // 70-80 range: 2 samples, 1 up, 1 down
            analysis["70-80"].ProbabilityUp.Should().Be(0.5);
        }

        [Fact]
        public void CalculateWithContextualFilter_GivenContextVariable_WhenFiltered_ThenAnalyzesSubset()
        {
            // Given - Mock movements with contextual data (isolated test)
            var movements = new List<PriceMovement>
            {
                new()
                {
                    MeasurementValue = 65m,
                    ATRMovement = -0.5m, // Price 100→95 = -0.5 ATR  
                    ContextualData = new Dictionary<string, decimal> { { "Daily_Score", 30m } }
                },
                new()
                {
                    MeasurementValue = 65m,
                    ATRMovement = 2.0m, // Price 100→120 = +2 ATR
                    ContextualData = new Dictionary<string, decimal> { { "Daily_Score", 70m } }
                }
            };

            // When - Analyze only when Daily_Score > 60
            var filteredCorrelation = _correlationService.CalculateWithContextualFilter(
                movements,
                contextVariable: "Daily_Score",
                contextThreshold: 60m,
                ComparisonOperator.GreaterThan);

            // Then
            filteredCorrelation.SampleSize.Should().Be(1); // Only one movement with Daily_Score > 60
            filteredCorrelation.AverageMovement.Should().Be(2.0m); // Filtered to only the +2 ATR movement
        }

        [Fact]
        public void RunFullAnalysis_GivenSimpleDataset_WhenAnalyzed_ThenReturnsComprehensiveResults()
        {
            // Given - Simple isolated test data
            var dataPoints = new List<DataPoint>
            {
                new() { Timestamp = DateTime.Now.AddMinutes(-60), MeasurementValue = 45m, Price = 100m, ATR = 10m },
                new() { Timestamp = DateTime.Now.AddMinutes(-30), MeasurementValue = 55m, Price = 110m, ATR = 10m },
                new() { Timestamp = DateTime.Now, MeasurementValue = 65m, Price = 120m, ATR = 10m }
            };

            var timeSeries = new TimeSeriesData { DataPoints = dataPoints };
            var request = new CorrelationAnalysisRequest
            {
                MeasurementId = "TestIndicator",
                TimeHorizons = new[] { TimeSpan.FromMinutes(30) },
                ATRTargets = new[] { 0.5m, 1.0m, 1.5m },
                MeasurementRanges = new List<(decimal, decimal)> 
                { 
                    (40, 50), (50, 60), (60, 70)
                }
            };

            // When
            var result = _correlationService.RunFullAnalysis(timeSeries, request);

            // Then
            result.Should().NotBeNull();
            result.MeasurementId.Should().Be("TestIndicator");
            
            // Should have correlations for each time horizon
            result.CorrelationsByTimeHorizon.Should().HaveCount(1);
            result.CorrelationsByTimeHorizon[TimeSpan.FromMinutes(30)].Should().NotBeNull();
            
            // Should have probability analysis for each range
            result.RangeAnalysis.Should().HaveCount(3); // (40-50), (50-60), (60-70)
            
            // With our simple test data, we have one point in each range
            result.RangeAnalysis.Should().ContainKey("40-50");
            result.RangeAnalysis.Should().ContainKey("50-60");
            result.RangeAnalysis.Should().ContainKey("60-70");
            
            // Should have ATR bucket analysis
            result.ATRBucketAnalysis.Should().NotBeEmpty();
            
            // Should calculate overall statistics
            result.OverallStatistics.Should().NotBeNull();
            result.OverallStatistics.TotalSamples.Should().BeGreaterThan(0);
            result.OverallStatistics.MeanATRMovement.Should().NotBe(0);
        }
    }
}