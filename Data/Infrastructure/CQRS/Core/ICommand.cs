using System;

namespace MedjCap.Data.Infrastructure.CQRS;

/// <summary>
/// Base interface for all commands in the CQRS pattern.
/// Commands represent write operations that modify system state.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Unique identifier for the command execution.
    /// </summary>
    Guid CommandId { get; }
    
    /// <summary>
    /// Timestamp when the command was created.
    /// </summary>
    DateTime CreatedAt { get; }
}

/// <summary>
/// Base interface for commands that return a result.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command</typeparam>
public interface ICommand<TResult> : ICommand
{
}

/// <summary>
/// Interface for handling commands in the CQRS pattern.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle</typeparam>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Handles the execution of a command.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for handling commands that return a result.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle</typeparam>
/// <typeparam name="TResult">The type of result returned by the command</typeparam>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Handles the execution of a command and returns a result.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Task representing the asynchronous operation with result</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation for commands.
/// </summary>
public abstract record BaseCommand : ICommand
{
    /// <summary>
    /// Unique identifier for the command execution.
    /// </summary>
    public Guid CommandId { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Timestamp when the command was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Base implementation for commands that return a result.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command</typeparam>
public abstract record BaseCommand<TResult> : BaseCommand, ICommand<TResult>
{
}