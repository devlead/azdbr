namespace AZDBR.Services;

/// <summary>
/// Syncs staging-only database principals from the renamed old database to the refreshed target database.
/// </summary>
public sealed class StagingPrincipalSyncService(
    ISqlExecutor sqlExecutor,
    ILogger<StagingPrincipalSyncService> logger)
{
    /// <summary>
    /// Copies missing roles, login-based users, and role memberships from the old database to the target database.
    /// </summary>
    /// <param name="request">Refresh request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SyncAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Syncing staging-only principals from {OldDatabase} to {TargetDatabase}.",
            request.OldDatabaseName,
            request.TargetDatabase);

        await SyncMissingRolesAsync(request, cancellationToken);
        await SyncMissingUsersAsync(request, cancellationToken);
        await SyncRoleMembershipsAsync(request, cancellationToken);
    }

    private async Task SyncMissingRolesAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var oldRoles = await sqlExecutor.GetCustomRoleNamesAsync(
            request.TargetServer,
            request.OldDatabaseName,
            request.TenantId,
            cancellationToken);

        var targetRoles = await sqlExecutor.GetCustomRoleNamesAsync(
            request.TargetServer,
            request.TargetDatabase,
            request.TenantId,
            cancellationToken);

        var targetRoleNames = new HashSet<string>(targetRoles, StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in oldRoles)
        {
            if (targetRoleNames.Contains(roleName))
            {
                continue;
            }

            await sqlExecutor.ExecuteNonQueryAsync(
                request.TargetServer,
                request.TargetDatabase,
                request.TenantId,
                SqlScriptBuilder.BuildCreateRoleScript(roleName),
                cancellationToken,
                request.CommandTimeout);
        }
    }

    private async Task SyncMissingUsersAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var oldUsers = await sqlExecutor.GetLoginUserNamesAsync(
            request.TargetServer,
            request.OldDatabaseName,
            request.TenantId,
            cancellationToken);

        var targetUsers = await sqlExecutor.GetLoginUserNamesAsync(
            request.TargetServer,
            request.TargetDatabase,
            request.TenantId,
            cancellationToken);

        var targetUserNames = new HashSet<string>(targetUsers, StringComparer.OrdinalIgnoreCase);

        foreach (var userName in oldUsers)
        {
            if (targetUserNames.Contains(userName))
            {
                continue;
            }

            await sqlExecutor.ExecuteNonQueryAsync(
                request.TargetServer,
                request.TargetDatabase,
                request.TenantId,
                SqlScriptBuilder.BuildCreateUserForLoginScript(userName),
                cancellationToken,
                request.CommandTimeout);
        }
    }

    private async Task SyncRoleMembershipsAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var oldMemberships = await sqlExecutor.GetRoleMembershipsAsync(
            request.TargetServer,
            request.OldDatabaseName,
            request.TenantId,
            cancellationToken);

        var targetMemberships = await sqlExecutor.GetRoleMembershipsAsync(
            request.TargetServer,
            request.TargetDatabase,
            request.TenantId,
            cancellationToken);

        var targetMembershipSet = new HashSet<DatabaseRoleMembership>(
            targetMemberships,
            DatabaseRoleMembershipComparer.Instance);

        foreach (var membership in oldMemberships)
        {
            if (targetMembershipSet.Contains(membership))
            {
                continue;
            }

            await sqlExecutor.ExecuteNonQueryAsync(
                request.TargetServer,
                request.TargetDatabase,
                request.TenantId,
                SqlScriptBuilder.BuildAddRoleMemberScript(membership.RoleName, membership.MemberName),
                cancellationToken,
                request.CommandTimeout);
        }
    }

    private sealed class DatabaseRoleMembershipComparer : IEqualityComparer<DatabaseRoleMembership>
    {
        public static DatabaseRoleMembershipComparer Instance { get; } = new();

        public bool Equals(DatabaseRoleMembership? x, DatabaseRoleMembership? y)
        {
            if (x is null || y is null)
            {
                return x == y;
            }

            return string.Equals(x.RoleName, y.RoleName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.MemberName, y.MemberName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(DatabaseRoleMembership obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.RoleName),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.MemberName));
    }
}
