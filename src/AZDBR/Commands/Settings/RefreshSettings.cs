namespace AZDBR.Commands.Settings;

/// <summary>
/// Settings for the refresh command.
/// </summary>
public sealed class RefreshSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the source logical server name.
    /// </summary>
    [CommandArgument(0, "<SOURCE_SERVER>")]
    [Description("Source Azure SQL logical server name.")]
    [ValidateString]
    public string SourceServer { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the source database name.
    /// </summary>
    [CommandArgument(1, "<SOURCE_DATABASE>")]
    [Description("Source Azure SQL database name.")]
    [ValidateString]
    public string SourceDatabase { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the target logical server name.
    /// </summary>
    [CommandArgument(2, "<TARGET_SERVER>")]
    [Description("Target Azure SQL logical server name.")]
    [ValidateString]
    public string TargetServer { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the target database name.
    /// </summary>
    [CommandArgument(3, "<TARGET_DATABASE>")]
    [Description("Target Azure SQL database name to refresh.")]
    [ValidateString]
    public string TargetDatabase { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional Entra tenant identifier.
    /// </summary>
    [CommandOption("--tenant-id")]
    [Description("Optional Microsoft Entra tenant identifier for authentication.")]
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets the optional service objective for the copied database.
    /// </summary>
    [CommandOption("--service-objective")]
    [Description("Optional service objective, for example S1 or ELASTIC_POOL(name=mypool).")]
    public string? ServiceObjective { get; init; }

    /// <summary>
    /// Gets or sets the optional backup storage redundancy setting.
    /// </summary>
    [CommandOption("--backup-storage-redundancy")]
    [Description("Optional backup storage redundancy: LOCAL, ZONE, GEO, or GEOZONE.")]
    public string? BackupStorageRedundancy { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the renamed old database should be kept.
    /// </summary>
    [CommandOption("--keep-old-database")]
    [Description("Keep the renamed old database instead of dropping it after success.")]
    public bool KeepOldDatabase { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to perform a dry run.
    /// </summary>
    [CommandOption("--dry-run")]
    [Description("Run preflight checks and print planned steps without making changes.")]
    public bool DryRun { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether confirmation should be skipped.
    /// </summary>
    [CommandOption("-y|--yes")]
    [Description("Skip interactive confirmation.")]
    public bool Yes { get; init; }

    /// <summary>
    /// Gets or sets the SQL command timeout in minutes.
    /// </summary>
    [CommandOption("--command-timeout-minutes")]
    [Description("SQL command timeout in minutes for non-copy operations (minimum: 30, default: 30).")]
    [DefaultValue(30)]
    public int CommandTimeoutMinutes { get; init; } = 30;

    /// <summary>
    /// Gets or sets the copy timeout in minutes.
    /// </summary>
    [CommandOption("--copy-timeout-minutes")]
    [Description("Maximum number of minutes to wait for database copy completion (minimum: 30).")]
    [DefaultValue(240)]
    public int CopyTimeoutMinutes { get; init; } = 240;

    /// <summary>
    /// Converts the settings to a refresh request model.
    /// </summary>
    /// <returns>The refresh request.</returns>
    public RefreshRequest ToRequest()
    {
        var copyTimeoutMinutes = Math.Max(CopyTimeoutMinutes, 30);
        var commandTimeoutMinutes = Math.Max(CommandTimeoutMinutes, 30);

        return new(
            SourceServer.Trim(),
            SourceDatabase.Trim(),
            TargetServer.Trim(),
            TargetDatabase.Trim(),
            string.IsNullOrWhiteSpace(TenantId) ? null : TenantId.Trim(),
            string.IsNullOrWhiteSpace(ServiceObjective) ? null : ServiceObjective.Trim(),
            string.IsNullOrWhiteSpace(BackupStorageRedundancy) ? null : BackupStorageRedundancy.Trim(),
            KeepOldDatabase,
            DryRun,
            Yes,
            TimeSpan.FromMinutes(copyTimeoutMinutes),
            TimeSpan.FromMinutes(commandTimeoutMinutes));
    }
}
