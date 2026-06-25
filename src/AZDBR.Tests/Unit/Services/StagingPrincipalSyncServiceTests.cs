namespace AZDBR.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="StagingPrincipalSyncService"/>.
/// </summary>
public sealed class StagingPrincipalSyncServiceTests
{
    [Test]
    public async Task SyncAsync_CreatesMissingPrincipalsOnTarget(CancellationToken cancellationToken)
    {
        var fakeSql = new FakeSqlExecutor();
        fakeSql.SeedDatabase("staging", "staging-db");
        fakeSql.SeedDatabase("staging", "staging-db-old");
        fakeSql.CustomRoles[("staging", "staging-db-old")] = ["staging_role"];
        fakeSql.LoginUsers[("staging", "staging-db-old")] = ["staging_user"];
        fakeSql.RoleMemberships[("staging", "staging-db-old")] =
            [new DatabaseRoleMembership("staging_role", "staging_user")];

        var service = new StagingPrincipalSyncService(fakeSql, NullLogger<StagingPrincipalSyncService>.Instance);
        var request = CreateRequest();

        await service.SyncAsync(request, cancellationToken);

        await Assert.That(fakeSql.ExecutedSql.Any(x => x.Sql.Contains("CREATE ROLE [staging_role]", StringComparison.OrdinalIgnoreCase))).IsTrue();
        await Assert.That(fakeSql.ExecutedSql.Any(x => x.Sql.Contains("CREATE USER [staging_user]", StringComparison.OrdinalIgnoreCase))).IsTrue();
        await Assert.That(fakeSql.ExecutedSql.Any(x => x.Sql.Contains("ALTER ROLE [staging_role] ADD MEMBER [staging_user]", StringComparison.OrdinalIgnoreCase))).IsTrue();
        await Assert.That(fakeSql.ExecutedSql.All(x => x.DatabaseName == "staging-db")).IsTrue();
        await Assert.That(fakeSql.ExecutedSql.All(x => !x.Sql.Contains("staging-db-old.sys", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    private static RefreshRequest CreateRequest() =>
        new(
            "prod-server",
            "prod-db",
            "staging",
            "staging-db",
            null,
            null,
            null,
            keepOldDatabase: false,
            dryRun: false,
            skipConfirmation: true,
            copyTimeout: TimeSpan.FromMinutes(30),
            commandTimeout: TimeSpan.FromMinutes(30));
}
