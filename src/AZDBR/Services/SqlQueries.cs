using System.Data;
using Dapper;

namespace AZDBR.Services;

/// <summary>
/// Dapper.AOT compiled SQL queries for Azure SQL metadata operations.
/// </summary>
[DapperAot]
internal static class SqlQueries
{
    /// <summary>
    /// Counts databases matching the specified name.
    /// </summary>
    /// <param name="connection">Open SQL connection.</param>
    /// <param name="databaseName">Database name.</param>
    /// <returns>Matching database count.</returns>
    internal static Task<int> CountDatabaseAsync(IDbConnection connection, string databaseName) =>
        connection.QuerySingleAsync<int>(
            """
            SELECT COUNT(1)
            FROM sys.databases
            WHERE [name] = @DatabaseName
            """,
            new { DatabaseName = databaseName });

    /// <summary>
    /// Gets the state description for a database.
    /// </summary>
    /// <param name="connection">Open SQL connection.</param>
    /// <param name="databaseName">Database name.</param>
    /// <returns>State description or null.</returns>
    internal static Task<string?> GetDatabaseStateAsync(IDbConnection connection, string databaseName) =>
        connection.QuerySingleOrDefaultAsync<string>(
            """
            SELECT [state_desc]
            FROM sys.databases
            WHERE [name] = @DatabaseName
            """,
            new { DatabaseName = databaseName });

    /// <summary>
    /// Gets the copy percent complete for a database.
    /// </summary>
    /// <param name="connection">Open SQL connection.</param>
    /// <param name="databaseName">Database name.</param>
    /// <returns>Percent complete or null.</returns>
    internal static Task<byte?> GetCopyPercentCompleteAsync(IDbConnection connection, string databaseName) =>
        connection.QuerySingleOrDefaultAsync<byte?>(
            """
            SELECT TOP (1) [percent_complete]
            FROM sys.dm_database_copies dc
            INNER JOIN sys.databases d ON dc.[database_id] = d.[database_id]
            WHERE d.[name] = @DatabaseName
            """,
            new { DatabaseName = databaseName });

    /// <summary>
    /// Gets custom database role names.
    /// </summary>
    /// <param name="connection">Open SQL connection.</param>
    /// <returns>Custom role names.</returns>
    internal static async Task<IReadOnlyList<string>> GetCustomRoleNamesAsync(IDbConnection connection) =>
        [.. await connection.QueryAsync<string>(
            """
            SELECT [name]
            FROM sys.database_principals
            WHERE [type] = 'R'
              AND [is_fixed_role] = 0
            """)];

    /// <summary>
    /// Gets login-based database user names.
    /// </summary>
    /// <param name="connection">Open SQL connection.</param>
    /// <returns>Login-based user names.</returns>
    internal static async Task<IReadOnlyList<string>> GetLoginUserNamesAsync(IDbConnection connection) =>
        [.. await connection.QueryAsync<string>(
            """
            SELECT [name]
            FROM sys.database_principals
            WHERE [type] IN ('S', 'U')
              AND [authentication_type] = 1
            """)];

    /// <summary>
    /// Gets database role memberships.
    /// </summary>
    /// <param name="connection">Open SQL connection.</param>
    /// <returns>Role memberships.</returns>
    internal static async Task<IReadOnlyList<DatabaseRoleMembership>> GetRoleMembershipsAsync(IDbConnection connection) =>
        [.. await connection.QueryAsync<DatabaseRoleMembership>(
            """
            SELECT role_p.[name] AS [RoleName],
                   member_p.[name] AS [MemberName]
            FROM sys.database_role_members rm
                INNER JOIN sys.database_principals role_p ON rm.[role_principal_id] = role_p.[principal_id]
                INNER JOIN sys.database_principals member_p ON rm.[member_principal_id] = member_p.[principal_id]
            WHERE member_p.[name] NOT IN ('dbo', 'guest', 'INFORMATION_SCHEMA', 'sys')
            """)];
}
