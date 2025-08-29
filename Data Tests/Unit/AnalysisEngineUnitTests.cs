using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.Analysis.Services;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.MachineLearning.Optimization.Models;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.Backtesting.Models;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.Statistics.Models;
using MedjCap.Data.Statistics.Correlation.Models;
using MedjCap.Data.Infrastructure.Models;

namespace MedjCap.Data.Tests.Unit
{
    /// <summary>
    /// Properly isolated unit tests for AnalysisEngine using mocked dependencies
    /// Demonstrates proper unit test isolation using Moq framework
    /// </summary>
    public class AnalysisEngineUnitTests
    {
        private readonly Mock<IDataCollector> _mockDataCollector;
        private readonly Mock<ICorrelationService> _mockCorrelationService;
        private readonly Mock<IMLBoundaryOptimizer> _mockBoundaryOptimizer;
        private readonly Mock<IBacktestService> _mockBacktestService;
        private readonly Mock<IAnalysisRepository> _mockRepository;
        private readonly IAnalysisEngine _analysisEngine;

        public AnalysisEngineUnitTests()
        {
            _mockDataCollector = new Mock<IDataCollector>();
            _mockCorrelationService = new Mock<ICorrelationService>();
            _mockBoundaryOptimizer = new Mock<IMLBoundaryOptimizer>();
            _mockBacktestService = new Mock<IBacktestService>();
            _mockRepository = new Mock<IAnalysisRepository>();

            _analysisEngine = new AnalysisEngine(
                _mockDataCollector.Object,
                _mockCorrelationService.Object,
                _mockBoundaryOptimizer.Object,
                _mockBacktestService.Object,
                _mockRepository.Object
            );
        }

        [Fact]
        public async Task RunAnalysisAsync_GivenValidRequest_WhenCalled_ThenReturnsAnalysisResult()
        {
            // Given - Mock the dependencies
            var request = new AnalysisRequest
            {
                MeasurementId = "TestMeasurement",
                TimeHorizons = new[] { TimeSpan.FromMinutes(30) },
                ATRTargets = new[] { 1.5m },
                Config = new AnalysisConfig
                {
                    InSample = new DateRange { Start = DateTime.Now.AddDays(-30), End = DateTime.Now }
                }
            };

            var mockDataPoints = new List<DataPoint>
            {
                new() { Timestamp = DateTime.Now.AddDays(-1), MeasurementValue = 50m, Price = 100m, ATR = 10m }
            };

            var mockTimeSeries = new TimeSeriesData { DataPoints = mockDataPoints };
            var mockMovements = new List<PriceMovement>
            {
                new() { MeasurementValue = 50m, ATRMovement = 1.2m }
            };

            var mockCorrelation = new CorrelationResult
            {
                Coefficient = 0.75,
                PValue = 0.02,
                IsStatisticallySignificant = true,
                SampleSize = 100,
                ConfidenceInterval = (0.6, 0.85),
                TStatistic = 2.5,
                DegreesOfFreedom = 98,
                StandardError = 0.1
            };

            var mockBoundaries = new List<OptimalBoundary>
            {
                new()
                {
                    RangeLow = 45m,
                    RangeHigh = 55m,
                    Confidence = 0.8,
                    ExpectedATRMove = 1.3m,
                    SampleCount = 50
                }
            };

            var mockWalkForward = new WalkForwardResults
            {
                WindowCount = 3,
                IsStable = true,
                StabilityScore = 0.9
            };

            // Setup mocks
            _mockDataCollector.Setup(x => x.GetDataByMeasurementId(request.MeasurementId))
                .Returns(mockDataPoints);

            _mockDataCollector.Setup(x => x.GetTimeSeriesData())
                .Returns(mockTimeSeries);

            _mockCorrelationService.Setup(x => x.CalculatePriceMovements(mockTimeSeries, It.IsAny<TimeSpan>()))
                .Returns(mockMovements);

            _mockCorrelationService.Setup(x => x.CalculateCorrelation(mockMovements, CorrelationType.Pearson))
                .Returns(mockCorrelation);

            _mockBoundaryOptimizer.Setup(x => x.FindOptimalBoundaries(mockMovements, request.ATRTargets[0], 5))
                .Returns(mockBoundaries);

            _mockBacktestService.Setup(x => x.CreateWalkForwardWindows(It.IsAny<DateRange>(), It.IsAny<int>()))
                .Returns(new List<WalkForwardWindow>());

            // When
            var result = await _analysisEngine.RunAnalysisAsync(request);

            // Then
            result.Should().NotBeNull();
            result.Status.Should().Be(AnalysisStatus.Completed);
            result.MeasurementId.Should().Be("TestMeasurement");
            result.CorrelationResults.Should().ContainKey(TimeSpan.FromMinutes(30));
            result.CorrelationResults[TimeSpan.FromMinutes(30)].Should().Be(mockCorrelation);
            result.OptimalBoundaries.Should().HaveCount(1);
            result.OptimalBoundaries.First().Should().Be(mockBoundaries.First());

            // Verify all dependencies were called as expected
            _mockDataCollector.Verify(x => x.GetDataByMeasurementId("TestMeasurement"), Times.Once);
            _mockDataCollector.Verify(x => x.GetTimeSeriesData(), Times.AtLeastOnce);
            _mockCorrelationService.Verify(x => x.CalculatePriceMovements(It.IsAny<TimeSeriesData>(), It.IsAny<TimeSpan>()), Times.AtLeastOnce);
            _mockCorrelationService.Verify(x => x.CalculateCorrelation(It.IsAny<List<PriceMovement>>(), CorrelationType.Pearson), Times.AtLeastOnce);
            _mockBoundaryOptimizer.Verify(x => x.FindOptimalBoundaries(It.IsAny<List<PriceMovement>>(), 1.5m, 5), Times.Once);
        }

        [Fact]
        public async Task RunAnalysisAsync_GivenNullRequest_WhenCalled_ThenThrowsArgumentNullException()
        {
            // When/Then
            await _analysisEngine.Invoking(async x => await x.RunAnalysisAsync(null!))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("request");
        }

        [Fact]
        public async Task RunAnalysisAsync_GivenEmptyMeasurementId_WhenCalled_ThenThrowsArgumentException()
        {
            // Given
            var request = new AnalysisRequest
            {
                MeasurementId = "", // Empty measurement ID
                TimeHorizons = new[] { TimeSpan.FromMinutes(30) },
                ATRTargets = new[] { 1.5m },
                Config = new AnalysisConfig()
            };

            // When/Then
            await _analysisEngine.Invoking(async x => await x.RunAnalysisAsync(request))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("*MeasurementId*");
        }

        [Fact]
        public async Task RunAnalysisAsync_GivenEmptyTimeHorizons_WhenCalled_ThenThrowsArgumentException()
        {
            // Given
            var request = new AnalysisRequest
            {
                MeasurementId = "TestMeasurement",
                TimeHorizons = Array.Empty<TimeSpan>(), // Empty time horizons
                ATRTargets = new[] { 1.5m },
                Config = new AnalysisConfig()
            };

            // When/Then
            await _analysisEngine.Invoking(async x => await x.RunAnalysisAsync(request))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("*TimeHorizons*");
        }

        [Fact]
        public async Task RunAnalysisAsync_GivenCancellationToken_WhenCancelled_ThenThrowsOperationCancelledException()
        {
            // Given
            var request = new AnalysisRequest
            {
                MeasurementId = "TestMeasurement",
                TimeHorizons = new[] { TimeSpan.FromMinutes(30) },
                ATRTargets = new[] { 1.5m },
                Config = new AnalysisConfig
                {
                    InSample = new DateRange { Start = DateTime.Now.AddDays(-30), End = DateTime.Now }
                }
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel(); // Cancel immediately

            // When/Then
            await _analysisEngine.Invoking(async x => await x.RunAnalysisAsync(request, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GetDataCollectorAsync_WhenCalled_ThenReturnsDataCollector()
        {
            // When
            var result = await _analysisEngine.GetDataCollectorAsync();

            // Then
            result.Should().Be(_mockDataCollector.Object);
        }

        [Fact]
        public async Task RunMultiMeasurementAnalysisAsync_GivenValidRequest_WhenCalled_ThenCallsAllDependencies()
        {
            // Given
            var request = new MultiMeasurementAnalysisRequest
            {
                MeasurementIds = new[] { "Measurement1", "Measurement2" },
                TimeHorizons = new[] { TimeSpan.FromMinutes(30) },
                ATRTargets = new[] { 1.5m },
                OptimizationTarget = OptimizationTarget.RiskAdjustedReturn
            };

            var mockData1 = new List<DataPoint>
            {
                new() { MeasurementValue = 50m, Price = 100m, ATR = 10m }
            };

            var mockData2 = new List<DataPoint>
            {
                new() { MeasurementValue = 60m, Price = 110m, ATR = 12m }
            };

            var mockMovements = new List<PriceMovement>
            {
                new() { MeasurementValue = 50m, ATRMovement = 1.2m }
            };

            var mockCorrelation = new CorrelationResult
            {
                Coefficient = 0.5,
                PValue = 0.05,
                IsStatisticallySignificant = true
            };

            var mockBoundaries = new List<OptimalBoundary>
            {
                new() { RangeLow = 45m, RangeHigh = 55m, Confidence = 0.7 }
            };

            // Setup mocks
            _mockDataCollector.Setup(x => x.GetDataByMeasurementId("Measurement1")).Returns(mockData1);
            _mockDataCollector.Setup(x => x.GetDataByMeasurementId("Measurement2")).Returns(mockData2);
            
            _mockCorrelationService.Setup(x => x.CalculatePriceMovements(It.IsAny<TimeSeriesData>(), It.IsAny<TimeSpan>()))
                .Returns(mockMovements);
            
            _mockCorrelationService.Setup(x => x.CalculateCorrelation(It.IsAny<List<PriceMovement>>(), CorrelationType.Pearson))
                .Returns(mockCorrelation);
            
            _mockBoundaryOptimizer.Setup(x => x.FindOptimalBoundaries(It.IsAny<List<PriceMovement>>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .Returns(mockBoundaries);

            // When
            var result = await _analysisEngine.RunMultiMeasurementAnalysisAsync(request);

            // Then
            result.Should().NotBeNull();
            result.OptimalWeights.Should().HaveCount(2);
            result.IndividualCorrelations.Should().HaveCount(2);

            // Verify interactions
            _mockDataCollector.Verify(x => x.GetDataByMeasurementId("Measurement1"), Times.Once);
            _mockDataCollector.Verify(x => x.GetDataByMeasurementId("Measurement2"), Times.Once);
        }

        [Fact]
        public void Constructor_GivenNullDataCollector_WhenCreated_ThenThrowsArgumentNullException()
        {
            // When/Then
            Action act = () => new AnalysisEngine(
                null!, // Null data collector
                _mockCorrelationService.Object,
                _mockBoundaryOptimizer.Object,
                _mockBacktestService.Object,
                _mockRepository.Object
            );

            act.Should().Throw<ArgumentNullException>().WithParameterName("dataCollector");
        }

        [Fact]
        public void Constructor_GivenNullCorrelationService_WhenCreated_ThenThrowsArgumentNullException()
        {
            // When/Then
            Action act = () => new AnalysisEngine(
                _mockDataCollector.Object,
                null!, // Null correlation service
                _mockBoundaryOptimizer.Object,
                _mockBacktestService.Object,
                _mockRepository.Object
            );

            act.Should().Throw<ArgumentNullException>().WithParameterName("correlationService");
        }
    }
}