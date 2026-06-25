using Microsoft.Data.SqlClient;

namespace AZDBR.Services;

/// <summary>
/// Builds Azure SQL connections authenticated with Entra ID tokens.
/// </summary>
public sealed class AzureSqlConnectionFactory(
    IAzureSqlTokenProvider tokenProvider,
    ILogger<AzureSqlConnectionFactory> logger) : IAzureSqlConnectionFactory
{
    /// <inheritdoc />
    public async Task<SqlConnection> CreateOpenConnectionAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var accessToken = await tokenProvider.GetAccessTokenAsync(tenantId, cancellationToken);
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = $"{serverName}.database.windows.net",
            InitialCatalog = databaseName,
            Encrypt = true,
            TrustServerCertificate = false
        }.ConnectionString;

        var connection = new SqlConnection(connectionString)
        {
            AccessToken = accessToken
        };

        logger.LogDebug("Opening connection to {Server}/{Database}.", serverName, databaseName);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
