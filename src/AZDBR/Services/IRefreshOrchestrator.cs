namespace AZDBR.Services;

/// <summary>
/// Coordinates the Azure SQL database refresh pipeline.
/// </summary>
public interface IRefreshOrchestrator
{
    /// <summary>
    /// Executes a database refresh operation.
    /// </summary>
    /// <param name="request">Refresh request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(RefreshRequest request, CancellationToken cancellationToken);
}
