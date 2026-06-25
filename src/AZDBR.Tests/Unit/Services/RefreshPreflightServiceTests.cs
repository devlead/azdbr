namespace AZDBR.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="RefreshPreflightService"/>.
/// </summary>
public sealed class RefreshPreflightServiceTests
{
    [Test]
    public async Task ValidateAndBuildPlanAsync_ThrowsWhenTargetDatabaseDoesNotExist(CancellationToken cancellationToken)
    {
        var fakeSql = new FakeSqlExecutor();
        fakeSql.SeedDatabase("prod-server", "prod-db");
        var service = new RefreshPreflightService(fakeSql, NullLogger<RefreshPreflightService>.Instance);
        var request = CreateRequest();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidateAndBuildPlanAsync(request, cancellationToken));

        await Assert.That(exception!.Message).Contains("must exist");
    }

    [Test]
    public async Task ValidateAndBuildPlanAsync_ThrowsWhenOldDatabaseAlreadyExists(CancellationToken cancellationToken)
    {
        var fakeSql = new FakeSqlExecutor();
        fakeSql.SeedDatabase("prod-server", "prod-db");
        fakeSql.SeedDatabase("staging", "staging-db");
        fakeSql.SeedDatabase("staging", "staging-db-old");
        var service = new RefreshPreflightService(fakeSql, NullLogger<RefreshPreflightService>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidateAndBuildPlanAsync(CreateRequest(), cancellationToken));

        await Assert.That(exception!.Message).Contains("Leftover database");
    }

    [Test]
    public async Task ValidateAndBuildPlanAsync_ReturnsPlanWhenPreflightPasses(CancellationToken cancellationToken)
    {
        var fakeSql = new FakeSqlExecutor();
        fakeSql.SeedDatabase("prod-server", "prod-db");
        fakeSql.SeedDatabase("staging", "staging-db");
        var service = new RefreshPreflightService(fakeSql, NullLogger<RefreshPreflightService>.Instance);

        var plan = await service.ValidateAndBuildPlanAsync(CreateRequest(), cancellationToken);

        await Assert.That(plan.SourceDescription).IsEqualTo("prod-server.prod-db");
        await Assert.That(plan.TargetDescription).IsEqualTo("staging.staging-db");
        await Assert.That(plan.Steps.Count).IsGreaterThan(5);
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
