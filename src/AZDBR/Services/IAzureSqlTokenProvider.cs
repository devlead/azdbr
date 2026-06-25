namespace AZDBR.Services;

/// <summary>
/// Provides Azure SQL access tokens using Entra ID.
/// </summary>
public interface IAzureSqlTokenProvider
{
    /// <summary>
    /// Gets an access token for Azure SQL Database.
    /// </summary>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token string.</returns>
    Task<string> GetAccessTokenAsync(string? tenantId, CancellationToken cancellationToken);
}
