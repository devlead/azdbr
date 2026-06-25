namespace AZDBR.Models;

/// <summary>
/// Describes a refresh operation request with source and target Azure SQL endpoints.
/// </summary>
public sealed record RefreshRequest
{
    /// <summary>
    /// Initializes a new refresh request.
    /// </summary>
    /// <param name="sourceServer">Source logical server name.</param>
    /// <param name="sourceDatabase">Source database name.</param>
    /// <param name="targetServer">Target logical server name.</param>
    /// <param name="targetDatabase">Target database name to replace.</param>
    /// <param name="tenantId">Optional Entra tenant identifier.</param>
    /// <param name="serviceObjective">Optional service objective for the copied database.</param>
    /// <param name="backupStorageRedundancy">Optional backup storage redundancy.</param>
    /// <param name="keepOldDatabase">When true, the renamed old database is retained.</param>
    /// <param name="dryRun">When true, only preflight and planned steps are reported.</param>
    /// <param name="skipConfirmation">When true, interactive confirmation is skipped.</param>
    /// <param name="copyTimeout">Maximum time to wait for database copy completion.</param>
    /// <param name="commandTimeout">SQL command timeout for non-copy operations.</param>
    public RefreshRequest(
        string sourceServer,
        string sourceDatabase,
        string targetServer,
        string targetDatabase,
        string? tenantId,
        string? serviceObjective,
        string? backupStorageRedundancy,
        bool keepOldDatabase,
        bool dryRun,
        bool skipConfirmation,
        TimeSpan copyTimeout,
        TimeSpan commandTimeout)
    {
        SourceServer = sourceServer;
        SourceDatabase = sourceDatabase;
        TargetServer = targetServer;
        TargetDatabase = targetDatabase;
        TenantId = tenantId;
        ServiceObjective = serviceObjective;
        BackupStorageRedundancy = backupStorageRedundancy;
        KeepOldDatabase = keepOldDatabase;
        DryRun = dryRun;
        SkipConfirmation = skipConfirmation;
        CopyTimeout = copyTimeout;
        CommandTimeout = commandTimeout;
    }

    /// <summary>
    /// Gets the source logical server name.
    /// </summary>
    public string SourceServer { get; }

    /// <summary>
    /// Gets the source database name.
    /// </summary>
    public string SourceDatabase { get; }

    /// <summary>
    /// Gets the target logical server name.
    /// </summary>
    public string TargetServer { get; }

    /// <summary>
    /// Gets the target database name.
    /// </summary>
    public string TargetDatabase { get; }

    /// <summary>
    /// Gets the renamed old target database name.
    /// </summary>
    public string OldDatabaseName => $"{TargetDatabase}-old";

    /// <summary>
    /// Gets the optional Entra tenant identifier.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the optional service objective for the copied database.
    /// </summary>
    public string? ServiceObjective { get; }

    /// <summary>
    /// Gets the optional backup storage redundancy setting.
    /// </summary>
    public string? BackupStorageRedundancy { get; }

    /// <summary>
    /// Gets a value indicating whether the old database should be kept after refresh.
    /// </summary>
    public bool KeepOldDatabase { get; }

    /// <summary>
    /// Gets a value indicating whether this is a dry run.
    /// </summary>
    public bool DryRun { get; }

    /// <summary>
    /// Gets a value indicating whether confirmation should be skipped.
    /// </summary>
    public bool SkipConfirmation { get; }

    /// <summary>
    /// Gets the maximum time to wait for copy completion.
    /// </summary>
    public TimeSpan CopyTimeout { get; }

    /// <summary>
    /// Gets the SQL command timeout for non-copy operations.
    /// </summary>
    public TimeSpan CommandTimeout { get; }
}
