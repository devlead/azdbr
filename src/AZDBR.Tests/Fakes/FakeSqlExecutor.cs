using System.Text.RegularExpressions;

namespace AZDBR.Tests.Fakes;

/// <summary>
/// In-memory SQL executor used by unit tests.
/// </summary>
public sealed partial class FakeSqlExecutor : ISqlExecutor
{
    private readonly Dictionary<(string Server, string Database), bool> _databases = new();

    [GeneratedRegex(@"ALTER DATABASE \[(?<current>[^\]]+)\] MODIFY NAME = \[(?<new>[^\]]+)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RenameDatabaseRegex();

    [GeneratedRegex(@"CREATE DATABASE \[(?<target>[^\]]+)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CreateDatabaseRegex();

    [GeneratedRegex(@"DROP DATABASE IF EXISTS \[(?<target>[^\]]+)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DropDatabaseRegex();

    /// <summary>
    /// Gets executed SQL batches in order.
    /// </summary>
    public List<ExecutedSql> ExecutedSql { get; } = [];

    /// <summary>
    /// Gets or sets database states by server and database name.
    /// </summary>
    public Dictionary<(string Server, string Database), string> DatabaseStates { get; } = new();

    /// <summary>
    /// Gets or sets copy percent complete values.
    /// </summary>
    public Dictionary<(string Server, string Database), byte?> CopyPercentComplete { get; } = new();

    /// <summary>
    /// Gets custom role names by server and database.
    /// </summary>
    public Dictionary<(string Server, string Database), List<string>> CustomRoles { get; } = new();

    /// <summary>
    /// Gets login-based user names by server and database.
    /// </summary>
    public Dictionary<(string Server, string Database), List<string>> LoginUsers { get; } = new();

    /// <summary>
    /// Gets role memberships by server and database.
    /// </summary>
    public Dictionary<(string Server, string Database), List<DatabaseRoleMembership>> RoleMemberships { get; } = new();

    /// <summary>
    /// Seeds a database as existing on a server.
    /// </summary>
    /// <param name="server">Server name.</param>
    /// <param name="database">Database name.</param>
    public void SeedDatabase(string server, string database) =>
        _databases[(server, database)] = true;

    /// <inheritdoc />
    public Task<int> ExecuteNonQueryAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        string sql,
        CancellationToken cancellationToken,
        TimeSpan? commandTimeout = null)
    {
        ExecutedSql.Add(new ExecutedSql(serverName, databaseName, sql));

        var renameMatch = RenameDatabaseRegex().Match(sql);
        if (renameMatch.Success)
        {
            _databases.Remove((serverName, renameMatch.Groups["current"].Value));
            _databases[(serverName, renameMatch.Groups["new"].Value)] = true;
        }

        var createMatch = CreateDatabaseRegex().Match(sql);
        if (createMatch.Success)
        {
            var target = createMatch.Groups["target"].Value;
            _databases[(serverName, target)] = true;
            DatabaseStates[(serverName, target)] = "ONLINE";
        }

        var dropMatch = DropDatabaseRegex().Match(sql);
        if (dropMatch.Success)
        {
            _databases.Remove((serverName, dropMatch.Groups["target"].Value));
        }

        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public Task<T?> ExecuteScalarAsync<T>(
        string serverName,
        string databaseName,
        string? tenantId,
        string sql,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Scalar execution is not used by fake tests.");

    /// <inheritdoc />
    public Task<bool> DatabaseExistsAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_databases.ContainsKey((serverName, databaseName)));

    /// <inheritdoc />
    public Task<string?> GetDatabaseStateAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        if (DatabaseStates.TryGetValue((serverName, databaseName), out var state))
        {
            return Task.FromResult<string?>(state);
        }

        return Task.FromResult(_databases.ContainsKey((serverName, databaseName)) ? "ONLINE" : null);
    }

    /// <inheritdoc />
    public Task<byte?> GetCopyPercentCompleteAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        if (CopyPercentComplete.TryGetValue((serverName, databaseName), out var percent))
        {
            return Task.FromResult(percent);
        }

        return Task.FromResult<byte?>(100);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetCustomRoleNamesAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(
            CustomRoles.GetValueOrDefault((serverName, databaseName)) ?? []);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetLoginUserNamesAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(
            LoginUsers.GetValueOrDefault((serverName, databaseName)) ?? []);

    /// <inheritdoc />
    public Task<IReadOnlyList<DatabaseRoleMembership>> GetRoleMembershipsAsync(
        string serverName,
        string databaseName,
        string? tenantId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DatabaseRoleMembership>>(
            RoleMemberships.GetValueOrDefault((serverName, databaseName)) ?? []);
}

/// <summary>
/// Represents an executed SQL batch in tests.
/// </summary>
/// <param name="ServerName">Server name.</param>
/// <param name="DatabaseName">Database name.</param>
/// <param name="Sql">SQL batch.</param>
public sealed record ExecutedSql(string ServerName, string DatabaseName, string Sql);
