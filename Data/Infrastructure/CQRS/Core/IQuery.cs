using System;

namespace MedjCap.Data.Infrastructure.CQRS;

/// <summary>
/// Base interface for all queries in the CQRS pattern.
/// Queries represent read operations that return data without modifying system state.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query</typeparam>
public interface IQuery<TResult>
{
    /// <summary>
    /// Unique identifier for the query execution.
    /// </summary>
    Guid QueryId { get; }
    
    /// <summary>
    /// Timestamp when the query was created.
    /// </summary>
    DateTime CreatedAt { get; }
}

/// <summary>
/// Interface for handling queries in the CQRS pattern.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle</typeparam>
/// <typeparam name="TResult">The type of result returned by the query</typeparam>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Handles the execution of a query and returns a result.
    /// </summary>
    /// <param name="query">The query to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Task representing the asynchronous operation with result</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation for queries.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query</typeparam>
public abstract record BaseQuery<TResult> : IQuery<TResult>
{
    /// <summary>
    /// Unique identifier for the query execution.
    /// </summary>
    public Guid QueryId { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Timestamp when the query was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}