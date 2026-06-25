using Microsoft.Data.SqlClient;

namespace AZDBR.Services;

/// <summary>
/// Executes SQL using authenticated Azure SQL connections.
/// </summary>
public sealed class SqlExecutor(
    IAzureSqlConnectionFactory connectionFactory,
    ILogger<SqlExecutor> logger) : ISqlExecutor
{
    /// <inheritdoc />
    public async Task<int> ExecuteNonQueryAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        string sql,
        CancellationToken cancellationToken,
        TimeSpan? commandTimeout = null)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(
            serverName,
            databaseName,
            tenantId,
            cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = SqlCommandTimeouts.ToSeconds(commandTimeout ?? SqlCommandTimeouts.Default);
        logger.LogDebug("Executing non-query on {Server}/{Database}.", serverName, databaseName);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> ExecuteScalarAsync<T>(
        string serverName,
        string databaseName,
        string? tenantId,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(
            serverName,
            databaseName,
            tenantId,
            cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = SqlCommandTimeouts.ToSeconds(SqlCommandTimeouts.Default);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            return default;
        }

        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public async Task<bool> DatabaseExistsAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(
            serverName,
            "master",
            tenantId,
            cancellationToken);

        var count = await SqlQueries.CountDatabaseAsync(connection, databaseName);
        return count > 0;
    }

    /// <inheritdoc />
    public async Task<string?> GetDatabaseStateAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(
            serverName,
            "master",
            tenantId,
            cancellationToken);

        return await SqlQueries.GetDatabaseStateAsync(connection, databaseName);
    }

    /// <inheritdoc />
    public async Task<byte?> GetCopyPercentCompleteAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(
            serverName,
            "master",
            tenantId,
            cancellationToken);

        return await SqlQueries.GetCopyPercentCompleteAsync(connection, databaseName);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCustomRoleNamesAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(
            serverName,
            databaseName,
            tenantId,
            cancellationToken);

        return await SqlQueries.GetCustomRoleNamesAsync(connection);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetLoginUserNamesAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(
            serverName,
            databaseName,
            tenantId,
            cancellationToken);

        return await SqlQueries.GetLoginUserNamesAsync(connection);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DatabaseRoleMembership>> GetRoleMembershipsAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(
            serverName,
            databaseName,
            tenantId,
            cancellationToken);

        return await SqlQueries.GetRoleMembershipsAsync(connection);
    }
}
