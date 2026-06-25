namespace AZDBR.Services;

/// <summary>
/// Performs preflight validation and builds refresh plans.
/// </summary>
public sealed class RefreshPreflightService(ISqlExecutor sqlExecutor, ILogger<RefreshPreflightService> logger)
{
    /// <summary>
    /// Validates the refresh request and returns a plan when successful.
    /// </summary>
    /// <param name="request">Refresh request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refresh plan.</returns>
    public async Task<RefreshPlan> ValidateAndBuildPlanAsync(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        ValidateIdentifiers(request);

        if (string.Equals(request.SourceServer, request.TargetServer, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.SourceDatabase, request.TargetDatabase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source and target database must not be the same.");
        }

        logger.LogInformation("Running preflight checks.");

        var sourceExists = await sqlExecutor.DatabaseExistsAsync(
            request.SourceServer,
            request.SourceDatabase,
            request.TenantId,
            cancellationToken);

        if (!sourceExists)
        {
            throw new InvalidOperationException(
                $"Source database '{request.SourceDatabase}' was not found on server '{request.SourceServer}'.");
        }

        var targetExists = await sqlExecutor.DatabaseExistsAsync(
            request.TargetServer,
            request.TargetDatabase,
            request.TenantId,
            cancellationToken);

        if (!targetExists)
        {
            throw new InvalidOperationException(
                $"Target database '{request.TargetDatabase}' must exist before refresh on server '{request.TargetServer}'.");
        }

        var oldExists = await sqlExecutor.DatabaseExistsAsync(
            request.TargetServer,
            request.OldDatabaseName,
            request.TenantId,
            cancellationToken);

        if (oldExists)
        {
            throw new InvalidOperationException(
                $"Leftover database '{request.OldDatabaseName}' exists on target server. Drop or rename it manually before retrying.");
        }

        return new RefreshPlan
        {
            SourceDescription = $"{request.SourceServer}.{request.SourceDatabase}",
            TargetDescription = $"{request.TargetServer}.{request.TargetDatabase}",
            OldDatabaseName = request.OldDatabaseName,
            DropOldDatabaseAfterSuccess = !request.KeepOldDatabase,
            Steps =
            [
                "Acquire Entra tokens for source and target servers",
                $"Kill active sessions on {request.TargetDatabase}",
                $"Rename {request.TargetDatabase} to {request.OldDatabaseName}",
                $"Verify {request.TargetDatabase} name is free",
                $"CREATE DATABASE {request.TargetDatabase} AS COPY OF {request.SourceServer}.{request.SourceDatabase}",
                "Fix orphaned login-based users",
                $"Sync staging-only roles, users, and memberships from {request.OldDatabaseName}",
                request.KeepOldDatabase
                    ? $"Keep renamed database {request.OldDatabaseName}"
                    : $"Drop renamed database {request.OldDatabaseName}"
            ]
        };
    }

    /// <summary>
    /// Verifies that the target database name is free after rename.
    /// </summary>
    /// <param name="request">Refresh request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task VerifyTargetNameIsFreeAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var targetExists = await sqlExecutor.DatabaseExistsAsync(
            request.TargetServer,
            request.TargetDatabase,
            request.TenantId,
            cancellationToken);

        if (targetExists)
        {
            throw new InvalidOperationException(
                $"Target database name '{request.TargetDatabase}' still exists after rename. Aborting copy.");
        }

        var oldExists = await sqlExecutor.DatabaseExistsAsync(
            request.TargetServer,
            request.OldDatabaseName,
            request.TenantId,
            cancellationToken);

        if (!oldExists)
        {
            throw new InvalidOperationException(
                $"Renamed database '{request.OldDatabaseName}' was not found after rename.");
        }
    }

    private static void ValidateIdentifiers(RefreshRequest request)
    {
        SqlIdentifierValidator.ToBracketedIdentifier(request.SourceServer);
        SqlIdentifierValidator.ToBracketedIdentifier(request.SourceDatabase);
        SqlIdentifierValidator.ToBracketedIdentifier(request.TargetServer);
        SqlIdentifierValidator.ToBracketedIdentifier(request.TargetDatabase);
        SqlIdentifierValidator.ToBracketedIdentifier(request.OldDatabaseName);
    }
}
