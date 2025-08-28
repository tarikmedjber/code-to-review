using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MedjCap.Data.Services;

/// <summary>
/// High-performance memory pool for array allocations used in data analysis operations.
/// Reduces garbage collection pressure by reusing array instances across operations.
/// </summary>
public interface IArrayMemoryPool
{
    /// <summary>
    /// Rents a double array of at least the specified minimum length.
    /// </summary>
    /// <param name="minimumLength">The minimum length required.</param>
    /// <returns>A rented array that must be returned via Return().</returns>
    double[] RentDoubleArray(int minimumLength);

    /// <summary>
    /// Rents a decimal array of at least the specified minimum length.
    /// </summary>
    /// <param name="minimumLength">The minimum length required.</param>
    /// <returns>A rented array that must be returned via Return().</returns>
    decimal[] RentDecimalArray(int minimumLength);

    /// <summary>
    /// Rents an int array of at least the specified minimum length.
    /// </summary>
    /// <param name="minimumLength">The minimum length required.</param>
    /// <returns>A rented array that must be returned via Return().</returns>
    int[] RentIntArray(int minimumLength);

    /// <summary>
    /// Returns a previously rented double array to the pool.
    /// </summary>
    /// <param name="array">The array to return. Can be null.</param>
    /// <param name="clearArray">Whether to clear the array contents before returning to pool.</param>
    void ReturnDoubleArray(double[]? array, bool clearArray = true);

    /// <summary>
    /// Returns a previously rented decimal array to the pool.
    /// </summary>
    /// <param name="array">The array to return. Can be null.</param>
    /// <param name="clearArray">Whether to clear the array contents before returning to pool.</param>
    void ReturnDecimalArray(decimal[]? array, bool clearArray = true);

    /// <summary>
    /// Returns a previously rented int array to the pool.
    /// </summary>
    /// <param name="array">The array to return. Can be null.</param>
    /// <param name="clearArray">Whether to clear the array contents before returning to pool.</param>
    void ReturnIntArray(int[]? array, bool clearArray = true);

    /// <summary>
    /// Gets current pool statistics for monitoring and diagnostics.
    /// </summary>
    /// <returns>Pool usage statistics.</returns>
    ArrayPoolStatistics GetStatistics();
}

/// <summary>
/// Statistics about array pool usage for monitoring and optimization.
/// </summary>
public record ArrayPoolStatistics
{
    public int DoubleArraysRented { get; init; }
    public int DecimalArraysRented { get; init; }
    public int IntArraysRented { get; init; }
    public int DoubleArraysInPool { get; init; }
    public int DecimalArraysInPool { get; init; }
    public int IntArraysInPool { get; init; }
    public long TotalBytesAllocated { get; init; }
    public long TotalBytesInPool { get; init; }
    public double PoolHitRatio { get; init; }
}

/// <summary>
/// Implementation of IArrayMemoryPool using .NET's ArrayPool for optimal performance.
/// Provides type-safe array pooling with statistics tracking and configuration options.
/// </summary>
public class ArrayMemoryPool : IArrayMemoryPool, IDisposable
{
    private readonly ArrayPool<double> _doublePool;
    private readonly ArrayPool<decimal> _decimalPool;
    private readonly ArrayPool<int> _intPool;
    
    // Statistics tracking
    private long _doubleRentCount;
    private long _decimalRentCount;
    private long _intRentCount;
    private long _doubleReturnCount;
    private long _decimalReturnCount;
    private long _intReturnCount;
    
    // For tracking arrays currently rented out
    private readonly ConcurrentDictionary<double[], int> _rentedDoubleArrays;
    private readonly ConcurrentDictionary<decimal[], int> _rentedDecimalArrays;
    private readonly ConcurrentDictionary<int[], int> _rentedIntArrays;

    private bool _disposed;

    /// <summary>
    /// Creates a new ArrayMemoryPool with default configuration.
    /// </summary>
    public ArrayMemoryPool()
    {
        // Use shared pools for optimal performance across the application
        _doublePool = ArrayPool<double>.Shared;
        _decimalPool = ArrayPool<decimal>.Shared;
        _intPool = ArrayPool<int>.Shared;
        
        _rentedDoubleArrays = new ConcurrentDictionary<double[], int>();
        _rentedDecimalArrays = new ConcurrentDictionary<decimal[], int>();
        _rentedIntArrays = new ConcurrentDictionary<int[], int>();
    }

    /// <summary>
    /// Creates a new ArrayMemoryPool with specified maximum array lengths for each pool.
    /// </summary>
    /// <param name="maxDoubleArrayLength">Maximum length for double arrays in pool.</param>
    /// <param name="maxDecimalArrayLength">Maximum length for decimal arrays in pool.</param>
    /// <param name="maxIntArrayLength">Maximum length for int arrays in pool.</param>
    public ArrayMemoryPool(int maxDoubleArrayLength, int maxDecimalArrayLength, int maxIntArrayLength)
    {
        _doublePool = ArrayPool<double>.Create(maxDoubleArrayLength, 50); // Max 50 arrays per bucket
        _decimalPool = ArrayPool<decimal>.Create(maxDecimalArrayLength, 50);
        _intPool = ArrayPool<int>.Create(maxIntArrayLength, 50);
        
        _rentedDoubleArrays = new ConcurrentDictionary<double[], int>();
        _rentedDecimalArrays = new ConcurrentDictionary<decimal[], int>();
        _rentedIntArrays = new ConcurrentDictionary<int[], int>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[] RentDoubleArray(int minimumLength)
    {
        if (minimumLength < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumLength), "Minimum length must be non-negative");

        var array = _doublePool.Rent(minimumLength);
        _rentedDoubleArrays.TryAdd(array, minimumLength);
        Interlocked.Increment(ref _doubleRentCount);
        
        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal[] RentDecimalArray(int minimumLength)
    {
        if (minimumLength < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumLength), "Minimum length must be non-negative");

        var array = _decimalPool.Rent(minimumLength);
        _rentedDecimalArrays.TryAdd(array, minimumLength);
        Interlocked.Increment(ref _decimalRentCount);
        
        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] RentIntArray(int minimumLength)
    {
        if (minimumLength < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumLength), "Minimum length must be non-negative");

        var array = _intPool.Rent(minimumLength);
        _rentedIntArrays.TryAdd(array, minimumLength);
        Interlocked.Increment(ref _intRentCount);
        
        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnDoubleArray(double[]? array, bool clearArray = true)
    {
        if (array == null) return;

        _rentedDoubleArrays.TryRemove(array, out _);
        
        if (clearArray)
            Array.Clear(array, 0, array.Length);
            
        _doublePool.Return(array, clearArray);
        Interlocked.Increment(ref _doubleReturnCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnDecimalArray(decimal[]? array, bool clearArray = true)
    {
        if (array == null) return;

        _rentedDecimalArrays.TryRemove(array, out _);
        
        if (clearArray)
            Array.Clear(array, 0, array.Length);
            
        _decimalPool.Return(array, clearArray);
        Interlocked.Increment(ref _decimalReturnCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnIntArray(int[]? array, bool clearArray = true)
    {
        if (array == null) return;

        _rentedIntArrays.TryRemove(array, out _);
        
        if (clearArray)
            Array.Clear(array, 0, array.Length);
            
        _intPool.Return(array, clearArray);
        Interlocked.Increment(ref _intReturnCount);
    }

    public ArrayPoolStatistics GetStatistics()
    {
        var doubleRented = (int)Interlocked.Read(ref _doubleRentCount);
        var decimalRented = (int)Interlocked.Read(ref _decimalRentCount);
        var intRented = (int)Interlocked.Read(ref _intRentCount);
        
        var doubleReturned = (int)Interlocked.Read(ref _doubleReturnCount);
        var decimalReturned = (int)Interlocked.Read(ref _decimalReturnCount);
        var intReturned = (int)Interlocked.Read(ref _intReturnCount);

        var totalRented = doubleRented + decimalRented + intRented;
        var totalReturned = doubleReturned + decimalReturned + intReturned;
        var hitRatio = totalRented > 0 ? (double)totalReturned / totalRented : 0.0;

        // Estimate bytes (rough calculation)
        var totalBytes = 
            (_rentedDoubleArrays.Values.Sum() * sizeof(double)) +
            (_rentedDecimalArrays.Values.Sum() * sizeof(decimal)) +
            (_rentedIntArrays.Values.Sum() * sizeof(int));

        return new ArrayPoolStatistics
        {
            DoubleArraysRented = doubleRented,
            DecimalArraysRented = decimalRented,
            IntArraysRented = intRented,
            DoubleArraysInPool = _rentedDoubleArrays.Count,
            DecimalArraysInPool = _rentedDecimalArrays.Count,
            IntArraysInPool = _rentedIntArrays.Count,
            TotalBytesAllocated = totalBytes,
            TotalBytesInPool = totalBytes, // Simplified - actual pool sizing is complex
            PoolHitRatio = hitRatio
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        // Return any outstanding arrays (development/debugging aid)
        foreach (var kvp in _rentedDoubleArrays)
            ReturnDoubleArray(kvp.Key, true);
        foreach (var kvp in _rentedDecimalArrays)
            ReturnDecimalArray(kvp.Key, true);
        foreach (var kvp in _rentedIntArrays)
            ReturnIntArray(kvp.Key, true);

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Convenience extensions for using array memory pools in a using statement pattern.
/// Provides automatic return of arrays when scope exits.
/// </summary>
public static class ArrayMemoryPoolExtensions
{
    /// <summary>
    /// Rents a double array and returns a disposable wrapper that automatically returns it.
    /// </summary>
    /// <param name="pool">The memory pool to rent from.</param>
    /// <param name="minimumLength">The minimum array length needed.</param>
    /// <returns>A disposable array rental that returns the array when disposed.</returns>
    public static RentedDoubleArray RentDoubleArrayDisposable(this IArrayMemoryPool pool, int minimumLength)
        => new(pool, minimumLength);

    /// <summary>
    /// Rents a decimal array and returns a disposable wrapper that automatically returns it.
    /// </summary>
    /// <param name="pool">The memory pool to rent from.</param>
    /// <param name="minimumLength">The minimum array length needed.</param>
    /// <returns>A disposable array rental that returns the array when disposed.</returns>
    public static RentedDecimalArray RentDecimalArrayDisposable(this IArrayMemoryPool pool, int minimumLength)
        => new(pool, minimumLength);

    /// <summary>
    /// Rents an int array and returns a disposable wrapper that automatically returns it.
    /// </summary>
    /// <param name="pool">The memory pool to rent from.</param>
    /// <param name="minimumLength">The minimum array length needed.</param>
    /// <returns>A disposable array rental that returns the array when disposed.</returns>
    public static RentedIntArray RentIntArrayDisposable(this IArrayMemoryPool pool, int minimumLength)
        => new(pool, minimumLength);
}

/// <summary>
/// Disposable wrapper for rented double arrays that automatically returns them to the pool.
/// </summary>
public readonly struct RentedDoubleArray : IDisposable
{
    private readonly IArrayMemoryPool _pool;
    public double[] Array { get; }

    internal RentedDoubleArray(IArrayMemoryPool pool, int minimumLength)
    {
        _pool = pool;
        Array = pool.RentDoubleArray(minimumLength);
    }

    public void Dispose() => _pool.ReturnDoubleArray(Array);
}

/// <summary>
/// Disposable wrapper for rented decimal arrays that automatically returns them to the pool.
/// </summary>
public readonly struct RentedDecimalArray : IDisposable
{
    private readonly IArrayMemoryPool _pool;
    public decimal[] Array { get; }

    internal RentedDecimalArray(IArrayMemoryPool pool, int minimumLength)
    {
        _pool = pool;
        Array = pool.RentDecimalArray(minimumLength);
    }

    public void Dispose() => _pool.ReturnDecimalArray(Array);
}

/// <summary>
/// Disposable wrapper for rented int arrays that automatically returns them to the pool.
/// </summary>
public readonly struct RentedIntArray : IDisposable
{
    private readonly IArrayMemoryPool _pool;
    public int[] Array { get; }

    internal RentedIntArray(IArrayMemoryPool pool, int minimumLength)
    {
        _pool = pool;
        Array = pool.RentIntArray(minimumLength);
    }

    public void Dispose() => _pool.ReturnIntArray(Array);
}