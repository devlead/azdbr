namespace AZDBR.Commands;

/// <summary>
/// Refreshes a target Azure SQL database from a source database.
/// </summary>
public sealed class RefreshCommand(
    IRefreshOrchestrator refreshOrchestrator,
    ILogger<RefreshCommand> logger) : AsyncCommand<RefreshSettings>
{
    /// <inheritdoc />
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        RefreshSettings settings,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Starting refresh from {SourceServer}.{SourceDatabase} to {TargetServer}.{TargetDatabase}.",
            settings.SourceServer,
            settings.SourceDatabase,
            settings.TargetServer,
            settings.TargetDatabase);

        await refreshOrchestrator.ExecuteAsync(settings.ToRequest(), cancellationToken);
        return 0;
    }
}
