namespace AZDBR.Tests.Unit.Commands;

/// <summary>
/// Tests for refresh command settings.
/// </summary>
public sealed class RefreshCommandTests
{
    [Test]
    public async Task ToRequest_MapsSettingsCorrectly(CancellationToken cancellationToken)
    {
        var settings = new RefreshSettings
        {
            SourceServer = " prod-server ",
            SourceDatabase = "prod-db",
            TargetServer = "staging",
            TargetDatabase = "staging-db",
            TenantId = "tenant",
            ServiceObjective = "S1",
            BackupStorageRedundancy = "LOCAL",
            KeepOldDatabase = true,
            DryRun = true,
            Yes = true,
            CopyTimeoutMinutes = 30,
            CommandTimeoutMinutes = 45
        };

        var request = settings.ToRequest();

        await Assert.That(request.SourceServer).IsEqualTo("prod-server");
        await Assert.That(request.TargetDatabase).IsEqualTo("staging-db");
        await Assert.That(request.TenantId).IsEqualTo("tenant");
        await Assert.That(request.ServiceObjective).IsEqualTo("S1");
        await Assert.That(request.BackupStorageRedundancy).IsEqualTo("LOCAL");
        await Assert.That(request.KeepOldDatabase).IsTrue();
        await Assert.That(request.DryRun).IsTrue();
        await Assert.That(request.SkipConfirmation).IsTrue();
        await Assert.That(request.CopyTimeout).IsEqualTo(TimeSpan.FromMinutes(30));
        await Assert.That(request.CommandTimeout).IsEqualTo(TimeSpan.FromMinutes(45));
    }
}
