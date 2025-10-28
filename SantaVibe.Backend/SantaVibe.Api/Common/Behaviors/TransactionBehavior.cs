using MediatR;
using SantaVibe.Api.Data;

namespace SantaVibe.Api.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps commands implementing ITransactionalCommand
/// with database transaction and execution strategy (retry logic)
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        ApplicationDbContext context,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply transaction for commands that implement ITransactionalCommand
        if (request is not ITransactionalCommand)
        {
            return await next();
        }

        // Use execution strategy to handle retries with transactions
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<ApplicationDbContext, TResponse>(
            state: _context,
            operation: async (dbContext, context, ct) =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(ct);

                try
                {
                    _logger.LogDebug(
                        "Starting transaction for command {CommandType}",
                        typeof(TRequest).Name);

                    var response = await next();

                    await transaction.CommitAsync(ct);

                    _logger.LogDebug(
                        "Transaction committed successfully for command {CommandType}",
                        typeof(TRequest).Name);

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Transaction rollback triggered for command {CommandType}",
                        typeof(TRequest).Name);

                    await transaction.RollbackAsync(ct);
                    throw;
                }
            },
            verifySucceeded: null,
            cancellationToken: cancellationToken);
    }
}
