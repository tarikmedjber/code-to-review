using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using MedjCap.Data.Services;
using MedjCap.Data.Services.OptimizationStrategies;
using MedjCap.Data.Storage;
using MedjCap.Data.Tests.Helpers;

namespace MedjCap.Data.Tests.Unit
{
    public class MLBoundaryOptimizerTests
    {
        private readonly IMLBoundaryOptimizer _optimizer;
        private readonly ICorrelationService _correlationService;
        private readonly IDataCollector _dataCollector;

        public MLBoundaryOptimizerTests()
        {
            var strategyFactory = new OptimizationStrategyFactory(TestConfigurationHelper.CreateDefaultOptimizationConfig());
            _optimizer = new MLBoundaryOptimizer(TestConfigurationHelper.CreateDefaultOptimizationConfig(), strategyFactory);
            _correlationService = new CorrelationService(TestConfigurationHelper.CreateDefaultStatisticalConfig());
            var timeSeriesStorage = new InMemoryTimeSeriesDataStorage();
            var multiDataStorage = new InMemoryMultiDataStorage();
            _dataCollector = new DataCollector(timeSeriesStorage, multiDataStorage);
        }

        [Fact]
        public void FindOptimalBoundaries_GivenPriceMovements_WhenOptimized_ThenReturnsOptimalRanges()
        {
            // Given - Data where 60-70 range predicts large moves
            var movements = GenerateTestMovements();

            // When
            var boundaries = _optimizer.FindOptimalBoundaries(
                movements,
                targetATRMove: 1.5m,
                maxRanges: 5);

            // Then
            boundaries.Should().NotBeEmpty();
            boundaries.Should().HaveCountLessOrEqualTo(5);

            var topBoundary = boundaries.First(); // Should be ordered by confidence
            topBoundary.RangeLow.Should().BeGreaterThanOrEqualTo(55);
            topBoundary.RangeHigh.Should().BeLessThanOrEqualTo(75);
            topBoundary.Confidence.Should().BeGreaterThan(0.6);
            topBoundary.ExpectedATRMove.Should().BeGreaterThan(1.0m);
            topBoundary.SampleCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void OptimizeWithDecisionTree_GivenMovements_WhenOptimized_ThenFindsSplitPoints()
        {
            // Given - Clear pattern: <50 moves down, 60-70 moves up big, >80 moves sideways
            var movements = new List<PriceMovement>
            {
                new() { MeasurementValue = 30m, ATRMovement = -2.0m },
                new() { MeasurementValue = 35m, ATRMovement = -1.8m },
                new() { MeasurementValue = 45m, ATRMovement = -1.0m },
                new() { MeasurementValue = 62m, ATRMovement = 2.0m },
                new() { MeasurementValue = 65m, ATRMovement = 2.5m },
                new() { MeasurementValue = 68m, ATRMovement = 2.2m },
                new() { MeasurementValue = 82m, ATRMovement = 0.1m },
                new() { MeasurementValue = 85m, ATRMovement = -0.1m },
            };

            // When
            var splitPoints = _optimizer.OptimizeWithDecisionTree(movements, maxDepth: 3);

            // Then
            splitPoints.Should().NotBeEmpty();
            splitPoints.Should().Contain(sp => sp >= 50 && sp <= 60); // Split between down and up
            splitPoints.Should().Contain(sp => sp >= 70 && sp <= 80); // Split between up and flat
        }

        [Fact]
        public void OptimizeWithClustering_GivenMovements_WhenClustered_ThenGroupsSimilarBehaviors()
        {
            // Given - Three distinct clusters of behavior
            var movements = new List<PriceMovement>();

            // Cluster 1: Low values, negative moves
            for (int i = 0; i < 10; i++)
                movements.Add(new() { MeasurementValue = 30 + i, ATRMovement = -1.5m + i * 0.1m });

            // Cluster 2: Mid values, positive moves
            for (int i = 0; i < 10; i++)
                movements.Add(new() { MeasurementValue = 60 + i, ATRMovement = 2.0m + i * 0.1m });

            // Cluster 3: High values, small moves
            for (int i = 0; i < 10; i++)
                movements.Add(new() { MeasurementValue = 85 + i, ATRMovement = 0.0m + i * 0.05m });

            // When
            var clusters = _optimizer.OptimizeWithClustering(movements, numberOfClusters: 3);

            // Then
            clusters.Should().HaveCount(3);

            // Each cluster should have distinct characteristics
            var orderedClusters = clusters.OrderBy(c => c.CenterMeasurement).ToList();

            // Low cluster
            orderedClusters[0].CenterMeasurement.Should().BeInRange(30, 45);
            orderedClusters[0].AverageATRMove.Should().BeLessThan(0);

            // Mid cluster  
            orderedClusters[1].CenterMeasurement.Should().BeInRange(60, 70);
            orderedClusters[1].AverageATRMove.Should().BeGreaterThan(1.5m);

            // High cluster
            orderedClusters[2].CenterMeasurement.Should().BeInRange(85, 95);
            orderedClusters[2].AverageATRMove.Should().BeInRange(-0.5m, 0.5m);
        }

        [Fact]
        public void OptimizeWithGradientSearch_GivenObjective_WhenOptimized_ThenMaximizesObjective()
        {
            // Given - Movements and objective to maximize hit rate for 2+ ATR moves
            var movements = GenerateTestMovements();

            var objective = new OptimizationObjective
            {
                Target = OptimizationTarget.LargeMoveProbability,
                MinATRMove = 2.0m,
                InitialRange = (50m, 70m)
            };

            // When
            var optimalRange = _optimizer.OptimizeWithGradientSearch(movements, objective);

            // Then
            optimalRange.Low.Should().BeGreaterThan(45);
            optimalRange.High.Should().BeLessThan(75);
            optimalRange.ObjectiveValue.Should().BeGreaterThan(0.5); // >50% hit rate

            // Optimal range should be better than initial
            var initialScore = CalculateObjective(movements, objective.InitialRange, objective.MinATRMove);
            optimalRange.ObjectiveValue.Should().BeGreaterThanOrEqualTo(initialScore);
        }

        [Fact]
        public void CombineOptimizationMethods_GivenMovements_WhenCombined_ThenSelectsBestApproach()
        {
            // Given
            var movements = GenerateTestMovements();
            var config = new MLOptimizationConfig
            {
                UseDecisionTree = true,
                UseClustering = true,
                UseGradientSearch = true,
                TargetATRMove = 1.5m,
                MaxRanges = 5,
                ValidationRatio = 0.2 // Use 20% for validation
            };

            // When
            var result = _optimizer.RunCombinedOptimization(movements, config);

            // Then
            result.Should().NotBeNull();
            result.BestMethod.Should().NotBeNullOrEmpty();
            result.OptimalBoundaries.Should().NotBeEmpty();
            result.ValidationScore.Should().BeGreaterThan(0);

            // Should have results from each method
            result.MethodResults.Should().ContainKey("DecisionTree");
            result.MethodResults.Should().ContainKey("Clustering");
            result.MethodResults.Should().ContainKey("GradientSearch");

            // Best boundaries should be from the winning method
            result.OptimalBoundaries.Should().BeEquivalentTo(
                result.MethodResults[result.BestMethod].Boundaries);
        }

        [Fact]
        public void ValidateBoundaries_GivenTrainTestSplit_WhenValidated_ThenReturnsOutOfSamplePerformance()
        {
            // Given - Boundaries discovered on training data
            var allMovements = GenerateTestMovements(200);
            var trainSize = 150;
            var trainMovements = allMovements.Take(trainSize).ToList();
            var testMovements = allMovements.Skip(trainSize).ToList();

            var boundaries = _optimizer.FindOptimalBoundaries(trainMovements, 1.5m, 3);

            // When
            var validation = _optimizer.ValidateBoundaries(boundaries, testMovements);

            // Then
            validation.Should().NotBeNull();
            validation.InSamplePerformance.Should().BeGreaterThan(0);
            validation.OutOfSamplePerformance.Should().BeGreaterThan(0);
            validation.PerformanceDegradation.Should().BeLessThan(0.5); // Less than 50% degradation

            foreach (var boundary in validation.BoundaryPerformance)
            {
                boundary.InSampleHitRate.Should().BeInRange(0, 1);
                boundary.OutOfSampleHitRate.Should().BeInRange(0, 1);
                boundary.IsStable.Should().Be(
                    Math.Abs(boundary.InSampleHitRate - boundary.OutOfSampleHitRate) < 0.3);
            }
        }

        [Fact]
        public void FindDynamicBoundaries_GivenTimeVaryingData_WhenAnalyzed_ThenDetectsRegimeChanges()
        {
            // Given - Data where optimal boundaries change over time
            var movements = new List<PriceMovement>();
            var baseTime = DateTime.Now;

            // First regime: 40-50 is optimal
            for (int i = 0; i < 50; i++)
            {
                var value = 30 + i % 30;
                var move = value >= 40 && value <= 50 ? 2.0m : 0.0m;
                movements.Add(new()
                {
                    StartTimestamp = baseTime.AddMinutes(i * 5),
                    MeasurementValue = value,
                    ATRMovement = move
                });
            }

            // Second regime: 60-70 is optimal
            for (int i = 50; i < 100; i++)
            {
                var value = 30 + i % 50;
                var move = value >= 60 && value <= 70 ? 2.5m : 0.0m;
                movements.Add(new()
                {
                    StartTimestamp = baseTime.AddMinutes(i * 5),
                    MeasurementValue = value,
                    ATRMovement = move
                });
            }

            // When
            var dynamicBoundaries = _optimizer.FindDynamicBoundaries(
                movements,
                windowSize: 50,
                stepSize: 25);

            // Then
            dynamicBoundaries.Should().NotBeEmpty();
            dynamicBoundaries.Should().HaveCountGreaterThan(1); // Multiple time windows

            // Early windows should favor 40-50 range
            var earlyWindow = dynamicBoundaries.First();
            earlyWindow.OptimalRange.Low.Should().BeInRange(35, 45);
            earlyWindow.OptimalRange.High.Should().BeInRange(45, 55);

            // Later windows should favor 60-70 range
            var lateWindow = dynamicBoundaries.Last();
            lateWindow.OptimalRange.Low.Should().BeInRange(55, 65);
            lateWindow.OptimalRange.High.Should().BeInRange(65, 75);
        }

        [Fact]
        public void OptimizeForMultipleObjectives_GivenConflictingGoals_WhenOptimized_ThenFindsPareto()
        {
            // Given - Multiple optimization objectives
            var movements = GenerateTestMovements();
            var objectives = new List<OptimizationObjective>
            {
                new() { Target = OptimizationTarget.HighestWinRate },
                new() { Target = OptimizationTarget.LargeMoveProbability, MinATRMove = 2.0m },
                new() { Target = OptimizationTarget.ConsistentResults }
            };

            // When
            var paretoFront = _optimizer.OptimizeForMultipleObjectives(movements, objectives);

            // Then
            paretoFront.Should().NotBeEmpty();

            // Each solution should be non-dominated
            foreach (var solution in paretoFront)
            {
                solution.Scores.Should().HaveCount(3); // One score per objective

                // At least one score should be competitive
                solution.Scores.Should().Contain(s => s > 0.5);

                // Should have the boundary that achieves these scores
                solution.Boundary.Should().NotBeNull();
                solution.Boundary.RangeLow.Should().BeLessThan(solution.Boundary.RangeHigh);
            }

            // Different solutions should represent different trade-offs
            var scores = paretoFront.Select(p => p.Scores[0]).ToList();
            scores.Distinct().Count().Should().BeGreaterThan(1);
        }

        // Helper methods
        private List<PriceMovement> GenerateTestMovements(int count = 100)
        {
            var movements = new List<PriceMovement>();
            var random = new Random(42);

            for (int i = 0; i < count; i++)
            {
                var value = 30 + random.Next(60);

                // Create pattern: 60-70 range tends to have large positive moves
                decimal atrMove;
                if (value >= 60 && value <= 70)
                    atrMove = 1.5m + (decimal)random.NextDouble();
                else if (value < 50)
                    atrMove = -1.0m + (decimal)random.NextDouble();
                else
                    atrMove = -0.5m + (decimal)random.NextDouble();

                movements.Add(new()
                {
                    MeasurementValue = value,
                    ATRMovement = atrMove,
                    StartTimestamp = DateTime.Now.AddMinutes(i * 5)
                });
            }

            return movements;
        }

        private double CalculateObjective(List<PriceMovement> movements, (decimal Low, decimal High) range, decimal minATR)
        {
            var inRange = movements.Where(m => m.MeasurementValue >= range.Low &&
                                               m.MeasurementValue <= range.High).ToList();
            if (!inRange.Any()) return 0;

            return (double)inRange.Count(m => Math.Abs(m.ATRMovement) >= minATR) / inRange.Count;
        }

        // Cross-Validation Tests
        [Fact]
        public void KFoldCrossValidation_GivenValidData_WhenK5_ThenReturnsValidResults()
        {
            // Given - Data with sufficient size for k-fold
            var movements = GenerateTestMovements(100);

            // When
            var result = _optimizer.KFoldCrossValidation(movements, k: 5);

            // Then
            result.Should().NotBeNull();
            result.FoldResults.Should().HaveCount(5);
            result.FoldScores.Should().HaveCount(5);
            result.MeanScore.Should().BeGreaterThanOrEqualTo(0);
            result.StdDevScore.Should().BeGreaterThanOrEqualTo(0);
            result.Config.KFolds.Should().Be(5);
            result.Config.Strategy.Should().Be(CrossValidationStrategy.KFold);

            // Each fold should have reasonable train/test split
            foreach (var fold in result.FoldResults)
            {
                fold.TrainingSampleCount.Should().BeGreaterThan(0);
                fold.ValidationSampleCount.Should().BeGreaterThan(0);
                fold.TrainingScore.Should().BeGreaterThanOrEqualTo(0);
                fold.ValidationScore.Should().BeGreaterThanOrEqualTo(0);
            }
        }

        [Fact]
        public void TimeSeriesKFold_GivenValidData_WhenK5_ThenReturnsValidResults()
        {
            // Given - Time series data with temporal order
            var movements = GenerateTimeSeriesMovements(80);

            // When
            var result = _optimizer.TimeSeriesKFold(movements, k: 5);

            // Then
            result.Should().NotBeNull();
            result.FoldResults.Should().NotBeEmpty();
            result.MeanScore.Should().BeGreaterThanOrEqualTo(0);
            result.Config.Strategy.Should().Be(CrossValidationStrategy.TimeSeriesExpanding);

            // Should preserve temporal order (expanding windows)
            for (int i = 1; i < result.FoldResults.Count; i++)
            {
                result.FoldResults[i].TrainingSampleCount.Should().BeGreaterThanOrEqualTo(result.FoldResults[i - 1].TrainingSampleCount);
            }
        }

        [Fact]
        public void ExpandingWindowValidation_GivenValidParameters_ThenReturnsTimeSeriesResults()
        {
            // Given - Time series data
            var movements = GenerateTimeSeriesMovements(60);

            // When
            var result = _optimizer.ExpandingWindowValidation(movements, initialSize: 0.3, stepSize: 0.2);

            // Then
            result.Should().NotBeNull();
            result.Should().BeOfType<TimeSeriesCrossValidationResult>();
            result.FoldResults.Should().NotBeEmpty();
            result.MeanScore.Should().BeGreaterThanOrEqualTo(0);
            (result.IsStationary == true || result.IsStationary == false).Should().BeTrue();
            result.TemporalDegradation.Should().BeGreaterThanOrEqualTo(0);
            result.OptimalLookbackWindow.Should().BeGreaterThan(TimeSpan.Zero);
            result.StationarityTests.Should().NotBeEmpty();

            // Each fold should have a period defined
            foreach (var fold in result.FoldResults)
            {
                fold.Period.Should().NotBeNull();
                fold.Period!.Start.Should().BeBefore(fold.Period.End);
            }
        }

        [Fact]
        public void RollingWindowValidation_GivenValidParameters_ThenReturnsTimeSeriesResults()
        {
            // Given - Time series data
            var movements = GenerateTimeSeriesMovements(70);

            // When
            var result = _optimizer.RollingWindowValidation(movements, windowSize: 0.4, stepSize: 0.1);

            // Then
            result.Should().NotBeNull();
            result.Should().BeOfType<TimeSeriesCrossValidationResult>();
            result.FoldResults.Should().NotBeEmpty();
            result.MeanScore.Should().BeGreaterThanOrEqualTo(0);
            (result.IsStationary == true || result.IsStationary == false).Should().BeTrue();
            result.TemporalDegradation.Should().BeGreaterThanOrEqualTo(0);
            result.OptimalLookbackWindow.Should().BeGreaterThan(TimeSpan.Zero);
            result.StationarityTests.Should().ContainKey("WindowConsistency");

            // Rolling windows should have consistent training sizes
            var trainingSizes = result.FoldResults.Select(f => f.TrainingSampleCount).Distinct().ToList();
            trainingSizes.Should().HaveCountLessOrEqualTo(2); // Should be mostly consistent
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void KFoldCrossValidation_GivenInvalidK_ThenThrowsException(int invalidK)
        {
            // Given
            var movements = GenerateTestMovements(50);

            // When/Then
            _optimizer.Invoking(o => o.KFoldCrossValidation(movements, invalidK))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(-0.1, 0.2)]
        [InlineData(1.1, 0.2)]
        [InlineData(0.3, -0.1)]
        [InlineData(0.3, 1.1)]
        public void ExpandingWindowValidation_GivenInvalidParameters_ThenThrowsException(double initialSize, double stepSize)
        {
            // Given
            var movements = GenerateTestMovements(50);

            // When/Then
            _optimizer.Invoking(o => o.ExpandingWindowValidation(movements, initialSize, stepSize))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void KFoldCrossValidation_GivenInsufficientData_WhenK5_ThenHandlesGracefully()
        {
            // Given - Data with less than k samples
            var movements = GenerateTestMovements(3);

            // When/Then - Should handle gracefully (either throw appropriate exception or handle with reduced folds)
            var result = _optimizer.KFoldCrossValidation(movements, k: 5);

            // Should work but with limited effectiveness due to small data size
            result.Should().NotBeNull();
            result.FoldResults.Should().HaveCount(5); // K-fold creates k folds regardless, but some may be empty
            
            // Most folds should have insufficient data for meaningful results
            var foldsWithData = result.FoldResults.Count(f => f.ValidationSampleCount > 0);
            foldsWithData.Should().BeLessOrEqualTo(3); // Only up to 3 folds can have validation data
        }

        [Fact]
        public void CrossValidationResults_ShouldIncludePerformanceMetrics()
        {
            // Given
            var movements = GenerateTestMovements(50);

            // When
            var kFoldResult = _optimizer.KFoldCrossValidation(movements, k: 3);
            var expandingResult = _optimizer.ExpandingWindowValidation(movements, 0.4, 0.2);

            // Then - Results should include comprehensive metrics
            kFoldResult.Metrics.Should().NotBeEmpty();
            kFoldResult.ConfidenceInterval.Lower.Should().BeLessOrEqualTo(kFoldResult.ConfidenceInterval.Upper);

            expandingResult.Metrics.Should().ContainKey("TemporalDegradation");
            expandingResult.Metrics.Should().ContainKey("EstimatedOptimalLookbackDays");
            expandingResult.StationarityTests.Should().ContainKey("PerformanceVariance");
        }

        private List<PriceMovement> GenerateTimeSeriesMovements(int count)
        {
            var movements = new List<PriceMovement>();
            var random = new Random(42);
            var baseTime = DateTime.Now.AddDays(-count);

            for (int i = 0; i < count; i++)
            {
                var value = 30 + random.Next(60);

                // Create time-dependent pattern: earlier data different from later data
                decimal atrMove;
                if (i < count / 2) // Earlier period
                {
                    if (value >= 60 && value <= 70)
                        atrMove = 1.2m + (decimal)random.NextDouble();
                    else
                        atrMove = -0.3m + (decimal)random.NextDouble();
                }
                else // Later period - different pattern
                {
                    if (value >= 50 && value <= 60)
                        atrMove = 1.5m + (decimal)random.NextDouble();
                    else
                        atrMove = -0.5m + (decimal)random.NextDouble();
                }

                movements.Add(new PriceMovement
                {
                    MeasurementValue = value,
                    ATRMovement = atrMove,
                    StartTimestamp = baseTime.AddHours(i * 4) // 4-hour intervals
                });
            }

            return movements.OrderBy(m => m.StartTimestamp).ToList();
        }
    }

}