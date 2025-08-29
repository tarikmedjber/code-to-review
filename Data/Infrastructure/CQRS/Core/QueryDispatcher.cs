using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using MedjCap.Data.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;

namespace MedjCap.Data.Infrastructure.CQRS;

/// <summary>
/// Interface for dispatching queries to their handlers.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Dispatches a query to its registered handler and returns a result.
    /// </summary>
    /// <typeparam name="TQuery">The type of query to dispatch.</typeparam>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="query">The query instance to handle.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous operation with result.</returns>
    Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResult>;
}

/// <summary>
/// Default implementation of query dispatcher using dependency injection.
/// Provides centralized query execution with caching and performance optimization.
/// </summary>
public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueryDispatcher> _logger;
    private readonly IMemoryCache? _cache;
    private readonly CachingConfig _cachingConfig;

    public QueryDispatcher(
        IServiceProvider serviceProvider, 
        ILogger<QueryDispatcher> logger,
        IMemoryCache? cache = null,
        IOptions<CachingConfig>? cachingConfig = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache;
        _cachingConfig = cachingConfig?.Value ?? new CachingConfig();
    }

    /// <summary>
    /// Dispatches a query to its appropriate handler and returns a result.
    /// Optionally uses caching for performance optimization.
    /// </summary>
    public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResult>
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var handler = _serviceProvider.GetService<IQueryHandler<TQuery, TResult>>();
        if (handler == null)
        {
            throw new InvalidOperationException($"No query handler registered for query type {typeof(TQuery).Name}");
        }

        var queryName = typeof(TQuery).Name;
        var cacheKey = $"query:{queryName}:{query.GetHashCode()}";

        // Try cache first if caching is enabled
        if (_cache != null && _cachingConfig.EnableCaching)
        {
            if (_cache.TryGetValue<TResult>(cacheKey, out var cachedResult))
            {
                _logger.LogDebug("Query {QueryName} cache HIT for key {CacheKey}", queryName, cacheKey);
                return cachedResult;
            }
        }

        _logger.LogInformation("Executing query {QueryName} with ID {QueryId}", queryName, query.QueryId);

        try
        {
            var startTime = DateTime.UtcNow;
            var result = await handler.HandleAsync(query, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Query {QueryName} completed successfully in {Duration}ms", 
                queryName, duration.TotalMilliseconds);

            // Cache the result if caching is enabled
            if (_cache != null && _cachingConfig.EnableCaching && result != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cachingConfig.AnalysisCache.TTL,
                    SlidingExpiration = TimeSpan.FromMinutes(10),
                    Size = 1
                };

                _cache.Set(cacheKey, result, cacheOptions);
                _logger.LogDebug("Query {QueryName} result cached with key {CacheKey}", queryName, cacheKey);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query {QueryName} failed with ID {QueryId}", queryName, query.QueryId);
            throw;
        }
    }
}