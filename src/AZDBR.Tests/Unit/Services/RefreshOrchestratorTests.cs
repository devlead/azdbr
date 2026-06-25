namespace AZDBR.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="RefreshOrchestrator"/>.
/// </summary>
public sealed class RefreshOrchestratorTests
{
    [Test]
    public async Task ExecuteAsync_DryRunDoesNotExecuteSql(CancellationToken cancellationToken)
    {
        var fakeSql = CreateSeededFakeSqlExecutor();
        var orchestrator = CreateOrchestrator(fakeSql);
        var request = CreateRequest(dryRun: true);

        await orchestrator.ExecuteAsync(request, cancellationToken);

        await Assert.That(fakeSql.ExecutedSql).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_RunsPipelineWhenConfirmed(CancellationToken cancellationToken)
    {
        var fakeSql = CreateSeededFakeSqlExecutor();
        var orchestrator = CreateOrchestrator(fakeSql);
        var request = CreateRequest(dryRun: false, skipConfirmation: true);

        await orchestrator.ExecuteAsync(request, cancellationToken);

        await Assert.That(fakeSql.ExecutedSql.Count).IsGreaterThan(4);
        await Assert.That(fakeSql.ExecutedSql.Any(x => x.Sql.Contains("MODIFY NAME", StringComparison.OrdinalIgnoreCase))).IsTrue();
        await Assert.That(fakeSql.ExecutedSql.Any(x => x.Sql.Contains("CREATE DATABASE", StringComparison.OrdinalIgnoreCase))).IsTrue();
        await Assert.That(fakeSql.ExecutedSql.Any(x => x.Sql.Contains("DROP DATABASE", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    private static FakeSqlExecutor CreateSeededFakeSqlExecutor()
    {
        var fakeSql = new FakeSqlExecutor();
        fakeSql.SeedDatabase("prod-server", "prod-db");
        fakeSql.SeedDatabase("staging", "staging-db");
        return fakeSql;
    }

    private static RefreshOrchestrator CreateOrchestrator(FakeSqlExecutor fakeSql)
    {
        var preflight = new RefreshPreflightService(fakeSql, NullLogger<RefreshPreflightService>.Instance);
        var principalSync = new StagingPrincipalSyncService(fakeSql, NullLogger<StagingPrincipalSyncService>.Instance);
        return new RefreshOrchestrator(
            preflight,
            principalSync,
            fakeSql,
            new FakeTokenProvider(),
            NullLogger<RefreshOrchestrator>.Instance);
    }

    private static RefreshRequest CreateRequest(bool dryRun, bool skipConfirmation = true) =>
        new(
            "prod-server",
            "prod-db",
            "staging",
            "staging-db",
            null,
            null,
            null,
            keepOldDatabase: false,
            dryRun,
            skipConfirmation,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(30));
}
