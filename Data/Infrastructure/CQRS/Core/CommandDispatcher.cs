using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MedjCap.Data.Infrastructure.CQRS;

/// <summary>
/// Interface for dispatching commands to their handlers.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches a command to its registered handler.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
    /// <param name="command">The command instance to handle.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand;

    /// <summary>
    /// Dispatches a command with a result to its registered handler.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
    /// <typeparam name="TResult">The type of result returned by the command.</typeparam>
    /// <param name="command">The command instance to handle.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous operation with result.</returns>
    Task<TResult> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>;
}

/// <summary>
/// Default implementation of command dispatcher using dependency injection.
/// Provides centralized command execution with logging and error handling.
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandDispatcher> _logger;

    public CommandDispatcher(IServiceProvider serviceProvider, ILogger<CommandDispatcher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Dispatches a command to its appropriate handler for execution.
    /// </summary>
    public async Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();
        if (handler == null)
        {
            throw new InvalidOperationException($"No command handler registered for command type {typeof(TCommand).Name}");
        }

        var commandName = typeof(TCommand).Name;
        _logger.LogInformation("Executing command {CommandName} with ID {CommandId}", commandName, command.CommandId);

        try
        {
            var startTime = DateTime.UtcNow;
            await handler.HandleAsync(command, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Command {CommandName} completed successfully in {Duration}ms", 
                commandName, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandName} failed with ID {CommandId}", commandName, command.CommandId);
            throw;
        }
    }

    /// <summary>
    /// Dispatches a command to its appropriate handler and returns a result.
    /// </summary>
    public async Task<TResult> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var handler = _serviceProvider.GetService<ICommandHandler<TCommand, TResult>>();
        if (handler == null)
        {
            throw new InvalidOperationException($"No command handler registered for command type {typeof(TCommand).Name}");
        }

        var commandName = typeof(TCommand).Name;
        _logger.LogInformation("Executing command {CommandName} with ID {CommandId}", commandName, command.CommandId);

        try
        {
            var startTime = DateTime.UtcNow;
            var result = await handler.HandleAsync(command, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Command {CommandName} completed successfully in {Duration}ms", 
                commandName, duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandName} failed with ID {CommandId}", commandName, command.CommandId);
            throw;
        }
    }
}