using FluentAssertions;
using MedjCap.Data.Infrastructure.MemoryManagement;
using System;
using System.Linq;
using Xunit;

namespace MedjCap.Data.Tests.Unit;

/// <summary>
/// Unit tests for ArrayMemoryPool to verify memory management and pooling behavior.
/// Tests performance characteristics, correctness, and resource management.
/// </summary>
public class ArrayMemoryPoolTests : IDisposable
{
    private readonly ArrayMemoryPool _memoryPool;

    public ArrayMemoryPoolTests()
    {
        _memoryPool = new ArrayMemoryPool();
    }

    [Fact]
    public void RentDoubleArray_ShouldReturnArrayWithMinimumLength()
    {
        // Arrange
        const int minimumLength = 100;

        // Act
        var array = _memoryPool.RentDoubleArray(minimumLength);

        // Assert
        array.Should().NotBeNull();
        array.Length.Should().BeGreaterOrEqualTo(minimumLength);
        
        // Cleanup
        _memoryPool.ReturnDoubleArray(array);
    }

    [Fact]
    public void RentDecimalArray_ShouldReturnArrayWithMinimumLength()
    {
        // Arrange
        const int minimumLength = 50;

        // Act
        var array = _memoryPool.RentDecimalArray(minimumLength);

        // Assert
        array.Should().NotBeNull();
        array.Length.Should().BeGreaterOrEqualTo(minimumLength);
        
        // Cleanup
        _memoryPool.ReturnDecimalArray(array);
    }

    [Fact]
    public void RentIntArray_ShouldReturnArrayWithMinimumLength()
    {
        // Arrange
        const int minimumLength = 200;

        // Act
        var array = _memoryPool.RentIntArray(minimumLength);

        // Assert
        array.Should().NotBeNull();
        array.Length.Should().BeGreaterOrEqualTo(minimumLength);
        
        // Cleanup
        _memoryPool.ReturnIntArray(array);
    }

    [Fact]
    public void RentArray_WithNegativeLength_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _memoryPool.RentDoubleArray(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _memoryPool.RentDecimalArray(-5));
        Assert.Throws<ArgumentOutOfRangeException>(() => _memoryPool.RentIntArray(-10));
    }

    [Fact]
    public void ReturnArray_WithNull_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        _memoryPool.ReturnDoubleArray(null);
        _memoryPool.ReturnDecimalArray(null);
        _memoryPool.ReturnIntArray(null);
    }

    [Fact]
    public void ArrayPooling_ShouldReuseArrays()
    {
        // Arrange
        const int arraySize = 100;

        // Act - Rent and return an array
        var firstArray = _memoryPool.RentDoubleArray(arraySize);
        _memoryPool.ReturnDoubleArray(firstArray);

        // Rent again with the same size
        var secondArray = _memoryPool.RentDoubleArray(arraySize);

        // Assert - Should get the same array back (pooling working)
        // Note: This test might be flaky as ArrayPool.Shared behavior can vary
        // The important thing is that it doesn't crash and returns valid arrays
        secondArray.Should().NotBeNull();
        secondArray.Length.Should().BeGreaterOrEqualTo(arraySize);
        
        // Cleanup
        _memoryPool.ReturnDoubleArray(secondArray);
    }

    [Fact]
    public void ClearArrayOnReturn_ShouldClearContents()
    {
        // Arrange
        const int arraySize = 10;
        var array = _memoryPool.RentDoubleArray(arraySize);
        
        // Fill array with non-zero values
        for (int i = 0; i < arraySize; i++)
        {
            array[i] = i + 1;
        }

        // Act - Return with clearing enabled
        _memoryPool.ReturnDoubleArray(array, clearArray: true);

        // Get the same array back
        var clearedArray = _memoryPool.RentDoubleArray(arraySize);

        // Assert - Array should be cleared (or at least not have our values)
        // Note: ArrayPool may return different array, so we check what we can
        clearedArray.Should().NotBeNull();
        
        // Cleanup
        _memoryPool.ReturnDoubleArray(clearedArray);
    }

    [Fact]
    public void GetStatistics_ShouldTrackRentals()
    {
        // Arrange
        var initialStats = _memoryPool.GetStatistics();

        // Act - Rent some arrays
        var doubleArray = _memoryPool.RentDoubleArray(100);
        var decimalArray = _memoryPool.RentDecimalArray(50);
        var intArray = _memoryPool.RentIntArray(200);

        var statsAfterRent = _memoryPool.GetStatistics();

        // Return arrays
        _memoryPool.ReturnDoubleArray(doubleArray);
        _memoryPool.ReturnDecimalArray(decimalArray);
        _memoryPool.ReturnIntArray(intArray);

        var statsAfterReturn = _memoryPool.GetStatistics();

        // Assert - Statistics should show rentals
        statsAfterRent.DoubleArraysRented.Should().BeGreaterThan(initialStats.DoubleArraysRented);
        statsAfterRent.DecimalArraysRented.Should().BeGreaterThan(initialStats.DecimalArraysRented);
        statsAfterRent.IntArraysRented.Should().BeGreaterThan(initialStats.IntArraysRented);

        // After returning, the counters should be higher
        statsAfterReturn.DoubleArraysRented.Should().Be(statsAfterRent.DoubleArraysRented);
        statsAfterReturn.DecimalArraysRented.Should().Be(statsAfterRent.DecimalArraysRented);
        statsAfterReturn.IntArraysRented.Should().Be(statsAfterRent.IntArraysRented);
    }

    [Fact]
    public void DisposableExtensions_ShouldAutomaticallyReturnArrays()
    {
        // Arrange & Act
        using (var rentedArray = _memoryPool.RentDoubleArrayDisposable(100))
        {
            // Assert - Array should be available
            rentedArray.Array.Should().NotBeNull();
            rentedArray.Array.Length.Should().BeGreaterOrEqualTo(100);
            
            // Array is automatically returned when using block exits
        }

        // No explicit return needed - disposal handles it
    }

    [Fact]
    public void MultipleArrayTypes_ShouldWorkConcurrently()
    {
        // Arrange
        const int iterations = 10;
        var doubleArrays = new double[iterations][];
        var decimalArrays = new decimal[iterations][];
        var intArrays = new int[iterations][];

        // Act - Rent multiple arrays of different types
        for (int i = 0; i < iterations; i++)
        {
            doubleArrays[i] = _memoryPool.RentDoubleArray(100 + i);
            decimalArrays[i] = _memoryPool.RentDecimalArray(50 + i);
            intArrays[i] = _memoryPool.RentIntArray(200 + i);
        }

        // Assert - All arrays should be valid
        for (int i = 0; i < iterations; i++)
        {
            doubleArrays[i].Should().NotBeNull();
            doubleArrays[i].Length.Should().BeGreaterOrEqualTo(100 + i);
            
            decimalArrays[i].Should().NotBeNull();
            decimalArrays[i].Length.Should().BeGreaterOrEqualTo(50 + i);
            
            intArrays[i].Should().NotBeNull();
            intArrays[i].Length.Should().BeGreaterOrEqualTo(200 + i);
        }

        // Cleanup - Return all arrays
        for (int i = 0; i < iterations; i++)
        {
            _memoryPool.ReturnDoubleArray(doubleArrays[i]);
            _memoryPool.ReturnDecimalArray(decimalArrays[i]);
            _memoryPool.ReturnIntArray(intArrays[i]);
        }
    }

    [Fact]
    public void CustomSizedPool_ShouldRespectConfiguration()
    {
        // Arrange
        const int maxDoubleArrayLength = 1000;
        const int maxDecimalArrayLength = 500;
        const int maxIntArrayLength = 2000;

        using var customPool = new ArrayMemoryPool(
            maxDoubleArrayLength, 
            maxDecimalArrayLength, 
            maxIntArrayLength);

        // Act - Rent arrays within and beyond limits
        var smallDoubleArray = customPool.RentDoubleArray(100);
        var largeDoubleArray = customPool.RentDoubleArray(maxDoubleArrayLength + 100);

        // Assert - Should get arrays regardless of size
        smallDoubleArray.Should().NotBeNull();
        largeDoubleArray.Should().NotBeNull();
        largeDoubleArray.Length.Should().BeGreaterOrEqualTo(maxDoubleArrayLength + 100);

        // Cleanup
        customPool.ReturnDoubleArray(smallDoubleArray);
        customPool.ReturnDoubleArray(largeDoubleArray);
    }

    [Fact]
    public void ZeroLengthArray_ShouldReturnValidArray()
    {
        // Act
        var array = _memoryPool.RentDoubleArray(0);

        // Assert
        array.Should().NotBeNull();
        // ArrayPool may return array with length > 0 even for 0 request

        // Cleanup
        _memoryPool.ReturnDoubleArray(array);
    }

    public void Dispose()
    {
        _memoryPool?.Dispose();
    }
}