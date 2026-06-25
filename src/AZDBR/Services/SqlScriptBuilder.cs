namespace AZDBR.Services;

/// <summary>
/// Builds SQL batches for the refresh pipeline.
/// </summary>
public static class SqlScriptBuilder
{
    /// <summary>
    /// Builds the SQL batch that kills active user sessions on the current database.
    /// </summary>
    /// <returns>Kill sessions SQL batch.</returns>
    public static string BuildKillSessionsScript() =>
        """
        DECLARE @sql NVARCHAR(MAX) = N'';
        SELECT @sql += N'KILL ' + CAST([session_id] AS NVARCHAR(10)) + N';'
        FROM sys.dm_exec_sessions
        WHERE [database_id] = DB_ID()
          AND [is_user_process] = 1
          AND [session_id] <> @@SPID;
        IF LEN(@sql) > 0
            EXEC sp_executesql @sql;
        """;

    /// <summary>
    /// Builds the SQL statement that renames a database.
    /// </summary>
    /// <param name="currentName">Current database name.</param>
    /// <param name="newName">New database name.</param>
    /// <returns>Rename database SQL statement.</returns>
    public static string BuildRenameDatabaseScript(string currentName, string newName)
    {
        var current = SqlIdentifierValidator.ToBracketedIdentifier(currentName);
        var renamed = SqlIdentifierValidator.ToBracketedIdentifier(newName);
        return $"ALTER DATABASE {current} MODIFY NAME = {renamed};";
    }

    /// <summary>
    /// Builds the SQL statement that creates a database copy.
    /// </summary>
    /// <param name="targetDatabase">Target database name.</param>
    /// <param name="sourceServer">Source server name.</param>
    /// <param name="sourceDatabase">Source database name.</param>
    /// <param name="serviceObjective">Optional service objective clause.</param>
    /// <param name="backupStorageRedundancy">Optional backup storage redundancy clause.</param>
    /// <returns>Create database copy SQL statement.</returns>
    public static string BuildCreateCopyScript(
        string targetDatabase,
        string sourceServer,
        string sourceDatabase,
        string? serviceObjective,
        string? backupStorageRedundancy)
    {
        var target = SqlIdentifierValidator.ToBracketedIdentifier(targetDatabase);
        var source = SqlIdentifierValidator.ToBracketedIdentifier(sourceServer);
        var sourceDb = SqlIdentifierValidator.ToBracketedIdentifier(sourceDatabase);

        var options = new List<string>();
        if (!string.IsNullOrWhiteSpace(serviceObjective))
        {
            options.Add($"SERVICE_OBJECTIVE = {serviceObjective.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(backupStorageRedundancy))
        {
            options.Add($"BACKUP_STORAGE_REDUNDANCY = '{backupStorageRedundancy.Trim().Trim('\'')}'");
        }

        var optionClause = options.Count == 0
            ? string.Empty
            : $" ({string.Join(", ", options)})";

        return $"CREATE DATABASE {target} AS COPY OF {source}.{sourceDb}{optionClause};";
    }

    /// <summary>
    /// Builds the SQL batch that remaps orphaned login-based users.
    /// </summary>
    /// <returns>Fix orphaned users SQL batch.</returns>
    public static string BuildFixOrphanedUsersScript() =>
        """
        DECLARE @username NVARCHAR(255);
        DECLARE @sql NVARCHAR(MAX);
        DECLARE user_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT [name]
            FROM sys.database_principals
            WHERE [type_desc] IN ('SQL_USER', 'WINDOWS_USER')
              AND [authentication_type] = 1;
        OPEN user_cursor;
        FETCH NEXT FROM user_cursor INTO @username;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sql = N'ALTER USER [' + REPLACE(@username, ']', ']]') + N'] WITH LOGIN = [' + REPLACE(@username, ']', ']]') + N'];';
            EXEC sp_executesql @sql;
            FETCH NEXT FROM user_cursor INTO @username;
        END;
        CLOSE user_cursor;
        DEALLOCATE user_cursor;
        """;

    /// <summary>
    /// Builds the SQL statement that creates a custom database role.
    /// </summary>
    /// <param name="roleName">Role name.</param>
    /// <returns>Create role SQL statement.</returns>
    public static string BuildCreateRoleScript(string roleName)
    {
        var role = SqlIdentifierValidator.ToBracketedIdentifier(roleName);
        return $"CREATE ROLE {role};";
    }

    /// <summary>
    /// Builds the SQL statement that creates a login-based database user.
    /// </summary>
    /// <param name="userName">User name.</param>
    /// <returns>Create user SQL statement.</returns>
    public static string BuildCreateUserForLoginScript(string userName)
    {
        var user = SqlIdentifierValidator.ToBracketedIdentifier(userName);
        return $"CREATE USER {user} FOR LOGIN {user};";
    }

    /// <summary>
    /// Builds the SQL statement that adds a member to a database role.
    /// </summary>
    /// <param name="roleName">Role name.</param>
    /// <param name="memberName">Member principal name.</param>
    /// <returns>Alter role SQL statement.</returns>
    public static string BuildAddRoleMemberScript(string roleName, string memberName)
    {
        var role = SqlIdentifierValidator.ToBracketedIdentifier(roleName);
        var member = SqlIdentifierValidator.ToBracketedIdentifier(memberName);
        return $"ALTER ROLE {role} ADD MEMBER {member};";
    }

    /// <summary>
    /// Builds the SQL statement that drops a database.
    /// </summary>
    /// <param name="databaseName">Database name to drop.</param>
    /// <returns>Drop database SQL statement.</returns>
    public static string BuildDropDatabaseScript(string databaseName)
    {
        var database = SqlIdentifierValidator.ToBracketedIdentifier(databaseName);
        return $"DROP DATABASE IF EXISTS {database};";
    }
}
