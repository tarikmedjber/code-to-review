using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.TimeSeries.Services;
using MedjCap.Data.Infrastructure.Storage;
using MedjCap.Data.TimeSeries.Storage.InMemory;
using MedjCap.Data.Infrastructure.Models;

namespace MedjCap.Data.Tests.Unit
{
    public class DataCollectorTests
    {
        private readonly IDataCollector _dataCollector;

        public DataCollectorTests()
        {
            var timeSeriesStorage = new InMemoryTimeSeriesDataStorage();
            var multiDataStorage = new InMemoryMultiDataStorage();
            _dataCollector = new DataCollector(timeSeriesStorage, multiDataStorage);
        }

        [Fact]
        public void AddSingleDataPoint_GivenValidMeasurementAndPrice_WhenAdded_ThenStoredCorrectly()
        {
            // Given
            var timestamp = new DateTime(2024, 1, 1, 10, 0, 0);
            var measurementId = "PriceVa_Score";
            var measurementValue = 65m;
            var price = 100m;
            var atr = 10m;

            // When
            _dataCollector.AddDataPoint(timestamp, measurementId, measurementValue, price, atr);
            var retrievedData = _dataCollector.GetDataPoints();

            // Then
            retrievedData.Should().HaveCount(1);
            var dataPoint = retrievedData.First();

            dataPoint.Timestamp.Should().Be(timestamp);
            dataPoint.MeasurementId.Should().Be("PriceVa_Score");
            dataPoint.MeasurementValue.Should().Be(65m);
            dataPoint.Price.Should().Be(100m);
            dataPoint.ATR.Should().Be(10m);
        }

        [Fact]
        public void AddDataPointWithContext_GivenContextualVariables_WhenAdded_ThenContextStored()
        {
            // Given
            var timestamp = new DateTime(2024, 1, 1, 10, 0, 0);
            var contextualData = new Dictionary<string, decimal>
            {
                { "Daily_PriceVa", 72m },
                { "Volatility_Regime", 2m } // High volatility
            };

            // When
            _dataCollector.AddDataPoint(
                timestamp,
                measurementId: "PriceVa_Score",
                measurementValue: 65m,
                price: 100m,
                atr: 10m,
                contextualData: contextualData
            );
            var dataPoint = _dataCollector.GetDataPoints().First();

            // Then
            dataPoint.ContextualData.Should().NotBeNull();
            dataPoint.ContextualData.Should().HaveCount(2);
            dataPoint.ContextualData["Daily_PriceVa"].Should().Be(72m);
            dataPoint.ContextualData["Volatility_Regime"].Should().Be(2m);
        }

        [Fact]
        public void AddMultipleDataPoint_GivenMultipleMeasurements_WhenAdded_ThenAllMeasurementsStored()
        {
            // Given
            var timestamp = new DateTime(2024, 1, 1, 10, 0, 0);
            var measurements = new Dictionary<string, decimal>
            {
                { "PriceVa_Score", 65m },
                { "FastVahaVaAtr_Score", 45m },
                { "PriceVa_Change", 5m }
            };
            var price = 100m;
            var atr = 10m;

            // When
            _dataCollector.AddMultipleDataPoint(timestamp, measurements, price, atr);
            var dataPoint = _dataCollector.GetMultiDataPoints().First();

            // Then
            dataPoint.Measurements.Should().HaveCount(3);
            dataPoint.Measurements["PriceVa_Score"].Should().Be(65m);
            dataPoint.Measurements["FastVahaVaAtr_Score"].Should().Be(45m);
            dataPoint.Measurements["PriceVa_Change"].Should().Be(5m);
            dataPoint.Price.Should().Be(100m);
            dataPoint.ATR.Should().Be(10m);
        }

        [Fact]
        public void GetDataByMeasurementId_GivenMixedData_WhenFiltered_ThenReturnsOnlyRequestedType()
        {
            // Given
            var baseTime = DateTime.Now;
            _dataCollector.AddDataPoint(baseTime,
                measurementId: "PriceVa_Score",
                measurementValue: 60m,
                price: 100m,
                atr: 10m);

            _dataCollector.AddDataPoint(baseTime.AddMinutes(5),
                measurementId: "FastVahaVaAtr_Score",
                measurementValue: 70m,
                price: 105m,
                atr: 10m);

            _dataCollector.AddDataPoint(baseTime.AddMinutes(10),
                measurementId: "PriceVa_Score",
                measurementValue: 65m,
                price: 110m,
                atr: 10m);

            // When
            var priceVaData = _dataCollector.GetDataByMeasurementId("PriceVa_Score");

            // Then
            priceVaData.Should().HaveCount(2);
            priceVaData.Should().OnlyContain(d => d.MeasurementId == "PriceVa_Score");
            priceVaData.Select(d => d.MeasurementValue).Should().BeEquivalentTo(new[] { 60m, 65m });
        }

        [Fact]
        public void GetDataByDateRange_GivenDataAcrossTime_WhenFiltered_ThenReturnsOnlyInRange()
        {
            // Given
            var startDate = new DateTime(2024, 1, 1, 9, 0, 0);
            var endDate = new DateTime(2024, 1, 1, 10, 0, 0);

            // Add data before, during, and after the range
            _dataCollector.AddDataPoint(
                startDate.AddMinutes(-30), // Before range
                measurementId: "PriceVa_Score",
                measurementValue: 50m,
                price: 100m,
                atr: 10m);

            _dataCollector.AddDataPoint(
                startDate.AddMinutes(30), // In range
                measurementId: "PriceVa_Score",
                measurementValue: 60m,
                price: 105m,
                atr: 10m);

            _dataCollector.AddDataPoint(
                endDate.AddMinutes(30), // After range
                measurementId: "PriceVa_Score",
                measurementValue: 70m,
                price: 110m,
                atr: 10m);

            // When
            var dateRange = new DateRange { Start = startDate, End = endDate };
            var filteredData = _dataCollector.GetDataByDateRange(dateRange);

            // Then
            filteredData.Should().HaveCount(1);
            filteredData.First().MeasurementValue.Should().Be(60m);
        }

        [Fact]
        public void GetDataPoints_GivenTimeSeriesData_WhenRetrieved_ThenOrderedByTimestamp()
        {
            // Given
            var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);

            // Add in random order
            _dataCollector.AddDataPoint(
                baseTime.AddMinutes(10),
                measurementId: "PriceVa_Score",
                measurementValue: 70m,
                price: 110m,
                atr: 10m);

            _dataCollector.AddDataPoint(
                baseTime,
                measurementId: "PriceVa_Score",
                measurementValue: 60m,
                price: 100m,
                atr: 10m);

            _dataCollector.AddDataPoint(
                baseTime.AddMinutes(5),
                measurementId: "PriceVa_Score",
                measurementValue: 65m,
                price: 105m,
                atr: 10m);

            // When
            var dataPoints = _dataCollector.GetDataPoints();

            // Then
            dataPoints.Should().HaveCount(3);
            dataPoints.Should().BeInAscendingOrder(d => d.Timestamp);
            dataPoints.First().MeasurementValue.Should().Be(60m);
            dataPoints.Last().MeasurementValue.Should().Be(70m);
        }

        [Fact]
        public void Clear_GivenExistingData_WhenCleared_ThenNoDataRemains()
        {
            // Given
            _dataCollector.AddDataPoint(
                DateTime.Now,
                measurementId: "PriceVa_Score",
                measurementValue: 65m,
                price: 100m,
                atr: 10m);

            _dataCollector.GetDataPoints().Should().HaveCount(1);

            // When
            _dataCollector.Clear();

            // Then
            _dataCollector.GetDataPoints().Should().BeEmpty();
            _dataCollector.GetMultiDataPoints().Should().BeEmpty();
        }

        [Fact]
        public void GetStatistics_GivenMultipleDataPoints_WhenRequested_ThenReturnsBasicStats()
        {
            // Given
            var baseTime = DateTime.Now;
            for (int i = 0; i < 10; i++)
            {
                _dataCollector.AddDataPoint(
                    baseTime.AddMinutes(i * 5),
                    measurementId: "PriceVa_Score",
                    measurementValue: 50m + i * 5,
                    price: 100m + i * 2,
                    atr: 10m);
            }

            // When
            var stats = _dataCollector.GetStatistics();

            // Then
            stats.Should().NotBeNull();
            stats.TotalDataPoints.Should().Be(10);
            stats.UniqueTimestamps.Should().Be(10);
            stats.UniqueMeasurementIds.Should().Contain("PriceVa_Score");
            stats.DateRange.Start.Should().Be(baseTime);
            stats.DateRange.End.Should().Be(baseTime.AddMinutes(45));
            stats.PriceRange.Should().NotBeNull();
            stats.PriceRange.Min.Should().Be(100m);
            stats.PriceRange.Max.Should().Be(118m);
        }

        [Fact]
        public void AddDataPoint_GivenDuplicateTimestampDifferentMeasurement_WhenAdded_ThenBothStored()
        {
            // Given - Multiple measurements at same timestamp
            var timestamp = new DateTime(2024, 1, 1, 10, 0, 0);

            // When
            _dataCollector.AddDataPoint(
                timestamp,
                measurementId: "PriceVa_Score",
                measurementValue: 65m,
                price: 100m,
                atr: 10m);

            _dataCollector.AddDataPoint(
                timestamp,
                measurementId: "FastVahaVaAtr_Score",
                measurementValue: 45m,
                price: 100m,
                atr: 10m);

            // Then
            var dataPoints = _dataCollector.GetDataPoints();
            dataPoints.Should().HaveCount(2);
            dataPoints.Select(d => d.MeasurementId).Distinct().Should().HaveCount(2);
        }

        [Fact]
        public void GetTimeSeriesForAnalysis_GivenData_WhenRequested_ThenReturnsStructuredTimeSeries()
        {
            // Given - Simulating data collection over time
            var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);

            // T0: Score = 62, Price = 100
            _dataCollector.AddDataPoint(baseTime,
                measurementId: "PriceVa_Score",
                measurementValue: 62m,
                price: 100m,
                atr: 10m);

            // T+30min: Score = 58, Price = 125
            _dataCollector.AddDataPoint(baseTime.AddMinutes(30),
                measurementId: "PriceVa_Score",
                measurementValue: 58m,
                price: 125m,
                atr: 10m);

            // T+60min: Score = 55, Price = 150
            _dataCollector.AddDataPoint(baseTime.AddMinutes(60),
                measurementId: "PriceVa_Score",
                measurementValue: 55m,
                price: 150m,
                atr: 10m);

            // When
            var timeSeries = _dataCollector.GetTimeSeriesData();

            // Then
            timeSeries.Should().NotBeNull();
            timeSeries.DataPoints.Should().HaveCount(3);
            timeSeries.TimeStep.Should().Be(TimeSpan.FromMinutes(30)); // Detected interval
            timeSeries.IsRegular.Should().BeTrue(); // Regular intervals

            // This data structure will be used by the analysis engine to calculate:
            // - From T0 (Score=62): Price moved 100→125 (+2.5 ATR) in 30min, 100→150 (+5 ATR) in 60min
            // - From T+30 (Score=58): Price moved 125→150 (+2.5 ATR) in 30min
        }

        [Fact]
        public void AddDataPoint_GivenCustomMeasurement_WhenAdded_ThenStoredCorrectly()
        {
            // Given - Testing that the package works with ANY measurement ID
            var timestamp = new DateTime(2024, 1, 1, 10, 0, 0);
            var customMeasurementId = "MyCustomIndicator_Value";
            var measurementValue = 42.5m;

            // When
            _dataCollector.AddDataPoint(
                timestamp,
                measurementId: customMeasurementId,
                measurementValue: measurementValue,
                price: 100m,
                atr: 10m);

            // Then
            var dataPoint = _dataCollector.GetDataPoints().First();
            dataPoint.MeasurementId.Should().Be(customMeasurementId);
            dataPoint.MeasurementValue.Should().Be(42.5m);

            // Proves the package is truly measurement-agnostic
        }
    }
}