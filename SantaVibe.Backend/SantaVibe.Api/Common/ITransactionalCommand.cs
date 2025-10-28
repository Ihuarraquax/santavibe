namespace SantaVibe.Api.Common;

/// <summary>
/// Marker interface for commands that require database transaction handling
/// Commands implementing this interface will be automatically wrapped in a transaction
/// by the TransactionBehavior pipeline behavior
/// </summary>
public interface ITransactionalCommand
{
}
