namespace AZDBR.Services;

/// <summary>
/// Executes SQL statements against Azure SQL databases.
/// </summary>
public interface ISqlExecutor
{
    /// <summary>
    /// Executes a non-query SQL batch.
    /// </summary>
    /// <param name="serverName">Logical server name.</param>
    /// <param name="databaseName">Database name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="sql">SQL batch to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="commandTimeout">Optional SQL command timeout. Defaults to 30 minutes.</param>
    /// <returns>The number of rows affected, when available.</returns>
    Task<int> ExecuteNonQueryAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        string sql,
        CancellationToken cancellationToken,
        TimeSpan? commandTimeout = null);

    /// <summary>
    /// Executes a scalar query.
    /// </summary>
    /// <typeparam name="T">Scalar result type.</typeparam>
    /// <param name="serverName">Logical server name.</param>
    /// <param name="databaseName">Database name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="sql">SQL query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scalar result or default.</returns>
    Task<T?> ExecuteScalarAsync<T>(
        string serverName,
        string databaseName,
        string? tenantId,
        string sql,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a database exists on the specified server.
    /// </summary>
    /// <param name="serverName">Logical server name.</param>
    /// <param name="databaseName">Database name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the database exists.</returns>
    Task<bool> DatabaseExistsAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the state description for a database.
    /// </summary>
    /// <param name="serverName">Logical server name.</param>
    /// <param name="databaseName">Database name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The state description or <c>null</c> when not found.</returns>
    Task<string?> GetDatabaseStateAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the copy percent complete for a database, when available.
    /// </summary>
    /// <param name="serverName">Logical server name.</param>
    /// <param name="databaseName">Database name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Percent complete or <c>null</c>.</returns>
    Task<byte?> GetCopyPercentCompleteAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets custom database role names from the specified database.
    /// </summary>
    /// <param name="serverName">Logical server name.</param>
    /// <param name="databaseName">Database name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Custom role names.</returns>
    Task<IReadOnlyList<string>> GetCustomRoleNamesAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets login-based database user names from the specified database.
    /// </summary>
    /// <param name="serverName">Logical server name.</param>
    /// <param name="databaseName">Database name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login-based user names.</returns>
    Task<IReadOnlyList<string>> GetLoginUserNamesAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets database role memberships from the specified database.
    /// </summary>
    /// <param name="serverName">Logical server name.</param>
    /// <param name="databaseName">Database name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Role memberships.</returns>
    Task<IReadOnlyList<DatabaseRoleMembership>> GetRoleMembershipsAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken);
}
