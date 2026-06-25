namespace AZDBR.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="SqlScriptBuilder"/>.
/// </summary>
public sealed class SqlScriptBuilderTests
{
    [Test]
    public async Task BuildCreateCopyScript_IncludesSourceAndTarget(CancellationToken cancellationToken)
    {
        var sql = SqlScriptBuilder.BuildCreateCopyScript(
            "staging-db",
            "prod-server",
            "prod-db",
            "S1",
            "LOCAL");

        await Assert.That(sql).Contains("CREATE DATABASE [staging-db]");
        await Assert.That(sql).Contains("AS COPY OF [prod-server].[prod-db]");
        await Assert.That(sql).Contains("SERVICE_OBJECTIVE = S1");
        await Assert.That(sql).Contains("BACKUP_STORAGE_REDUNDANCY = 'LOCAL'");
    }

    [Test]
    public async Task BuildFixOrphanedUsersScript_UsesSpExecuteSql(CancellationToken cancellationToken)
    {
        var sql = SqlScriptBuilder.BuildFixOrphanedUsersScript();

        await Assert.That(sql).Contains("EXEC sp_executesql @sql");
        await Assert.That(sql).DoesNotContain("EXEC(N'ALTER USER");
    }

    [Test]
    public async Task BuildAddRoleMemberScript_UsesBracketedIdentifiers(CancellationToken cancellationToken)
    {
        var sql = SqlScriptBuilder.BuildAddRoleMemberScript("staging-role", "staging-user");

        await Assert.That(sql).IsEqualTo("ALTER ROLE [staging-role] ADD MEMBER [staging-user];");
    }
}
