# MedjCap.Data Performance Benchmarks

This directory contains comprehensive performance benchmarks for the MedjCap.Data financial analysis system, built using BenchmarkDotNet.

## Overview

The benchmark suite covers key performance areas:

- **Statistical Calculations** - Correlation analysis with varying data sizes
- **ML Optimization Algorithms** - Strategy pattern performance across different algorithms  
- **Outlier Detection** - Multiple detection methods and handling strategies
- **Data Collection** - Storage and retrieval performance at scale
- **Memory Allocation** - GC pressure and memory efficiency analysis

## Running Benchmarks

### Run All Benchmarks
```bash
cd tests/MedjCap.Data.Tests
dotnet run --project . --configuration Release --framework net9.0 Benchmarks/BenchmarkRunner.cs
```

### Run Specific Benchmark Suites
```bash
# Correlation service benchmarks
dotnet run correlation

# ML boundary optimizer benchmarks  
dotnet run ml

# Outlier detection benchmarks
dotnet run outlier

# Data collection benchmarks
dotnet run data

# Memory allocation benchmarks
dotnet run memory
```

### Direct BenchmarkDotNet Execution
```bash
dotnet run -c Release -- --filter "*CorrelationService*"
dotnet run -c Release -- --filter "*MLBoundaryOptimizer*"
dotnet run -c Release -- --filter "*OutlierDetection*"
```

## Benchmark Categories

### 1. CorrelationServiceBenchmarks
Tests correlation calculation performance across different scenarios:

- **Data Size Scaling**: 100 to 50,000 data points
- **Outlier Detection Impact**: With and without outlier handling
- **Correlation Types**: Pearson, Spearman, Kendall coefficients
- **Preprocessing**: Price movement calculation performance

**Key Metrics:**
- Execution time vs data size
- Memory allocation patterns
- Outlier detection overhead (~10-15% performance cost)

### 2. MLBoundaryOptimizerBenchmarks  
Evaluates ML optimization algorithm performance:

- **Strategy Pattern Performance**: Individual vs combined algorithms
- **Algorithm Comparison**: DecisionTree, Clustering, GradientSearch
- **Data Size Impact**: 200 to 5,000 sample scaling
- **Configuration Variations**: Different algorithm parameters

**Key Metrics:**
- Strategy execution time
- Memory usage per algorithm
- Scalability characteristics

### 3. OutlierDetectionBenchmarks
Measures outlier detection algorithm efficiency:

- **Detection Methods**: IQR, Z-Score, Modified Z-Score, Isolation Forest, Ensemble
- **Handling Strategies**: Remove, Cap, Replace, Transform, Flag
- **Pipeline Performance**: Complete detect + handle workflows
- **Data Quality Assessment**: Comprehensive analysis performance

**Key Metrics:**
- Algorithm execution time
- Method accuracy vs performance trade-offs
- Memory allocation during detection

### 4. DataCollectionBenchmarks
Evaluates data storage and retrieval performance:

- **Storage Operations**: Time series and multi-data point storage
- **Retrieval Performance**: Range queries and bulk operations  
- **Bulk Processing**: Chunked data processing efficiency
- **Memory Usage**: Storage overhead analysis

**Key Metrics:**
- Storage throughput (operations/second)
- Query performance
- Memory efficiency

### 5. MemoryAllocationBenchmarks
Focuses on memory efficiency and GC pressure:

- **Allocation Patterns**: Object creation and disposal
- **Pipeline Memory Usage**: End-to-end processing memory footprint
- **GC Pressure**: Garbage collection frequency and impact
- **Memory Leaks**: Long-running operation memory stability

**Key Metrics:**
- Bytes allocated per operation
- Gen 0/1/2 GC collections
- Memory cleanup efficiency

## Performance Baselines

### Expected Performance Ranges

| Operation | Data Size | Execution Time | Memory Usage |
|-----------|-----------|----------------|--------------|
| Correlation (Pearson) | 1,000 points | < 1ms | < 50KB |
| Correlation (Pearson) | 10,000 points | < 10ms | < 500KB |
| Outlier Detection (IQR) | 1,000 points | < 2ms | < 25KB |
| ML Optimization (Single) | 1,000 points | < 100ms | < 1MB |
| ML Optimization (Combined) | 1,000 points | < 300ms | < 2MB |

### Performance Targets

- **Sub-second Response**: All operations under 1,000 data points
- **Linear Scaling**: O(n) or better complexity for most operations
- **Memory Efficiency**: < 1KB per data point for most operations
- **GC Pressure**: Minimal Gen 1/2 collections during normal operations

## Interpreting Results

### BenchmarkDotNet Output

Results are exported in multiple formats:
- **HTML Reports**: `BenchmarkDotNet.Artifacts/results/*.html`
- **CSV Data**: `BenchmarkDotNet.Artifacts/results/*.csv`
- **JSON Format**: `BenchmarkDotNet.Artifacts/results/*.json`

### Key Metrics to Monitor

1. **Mean Execution Time**: Average performance across iterations
2. **Memory Allocated**: Total bytes allocated per operation
3. **Gen 0/1/2 Collections**: Garbage collection pressure
4. **Rank**: Relative performance ranking
5. **Ratio**: Performance compared to baseline

### Performance Regression Detection

Monitor these indicators for performance regressions:

- **> 20% increase** in execution time for same data size
- **> 50% increase** in memory allocation
- **New Gen 1/2 collections** in previously clean operations
- **Non-linear scaling** where O(n) was expected

## Continuous Performance Monitoring

### Integration with CI/CD

Add benchmark runs to your CI pipeline:

```yaml
- name: Run Performance Benchmarks  
  run: |
    cd tests/MedjCap.Data.Tests
    dotnet run -c Release Benchmarks/BenchmarkRunner.cs
    
- name: Upload Benchmark Results
  uses: actions/upload-artifact@v3
  with:
    name: benchmark-results
    path: tests/MedjCap.Data.Tests/BenchmarkDotNet.Artifacts/
```

### Performance Dashboard

Consider integrating results with monitoring tools:
- Export CSV results to time-series database
- Create dashboards for performance trend analysis
- Set up alerts for significant performance degradation

## Troubleshooting

### Common Issues

1. **High Memory Usage**: Check for memory leaks in long-running benchmarks
2. **Inconsistent Results**: Ensure Release configuration and stable environment
3. **GC Interference**: Use `[MemoryDiagnoser]` to identify allocation hotspots
4. **Outliers in Results**: Run multiple iterations and check for external interference

### Optimization Tips

1. **Pre-allocate Collections**: Use `List<T>(capacity)` when size is known
2. **Avoid Boxing**: Use generic methods and value types when possible
3. **Minimize LINQ Chains**: Consider foreach loops for performance-critical paths
4. **Reuse Objects**: Pool expensive objects when appropriate

## Contributing

When adding new benchmarks:

1. **Follow Naming Convention**: `[Component][Operation]Benchmarks.cs`
2. **Include Memory Diagnostics**: Add `[MemoryDiagnoser]` attribute
3. **Use Realistic Data**: Generate data that represents actual usage patterns
4. **Document Expected Results**: Add comments about expected performance characteristics
5. **Test Multiple Scales**: Include small, medium, and large dataset tests