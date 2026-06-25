using Microsoft.Data.SqlClient;

namespace AZDBR.Services;

/// <summary>
/// Creates authenticated Azure SQL connections.
/// </summary>
public interface IAzureSqlConnectionFactory
{
    /// <summary>
    /// Creates and opens a connection to the specified server and database.
    /// </summary>
    /// <param name="serverName">Logical server name without domain suffix.</param>
    /// <param name="databaseName">Database name or <c>master</c>.</param>
    /// <param name="tenantId">Optional Entra tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open SQL connection.</returns>
    Task<SqlConnection> CreateOpenConnectionAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken);
}
