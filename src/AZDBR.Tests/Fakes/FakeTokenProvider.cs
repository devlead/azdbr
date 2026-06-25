namespace AZDBR.Tests.Fakes;

/// <summary>
/// Fake token provider for tests.
/// </summary>
public sealed class FakeTokenProvider : IAzureSqlTokenProvider
{
    /// <inheritdoc />
    public Task<string> GetAccessTokenAsync(string? tenantId, CancellationToken cancellationToken) =>
        Task.FromResult("test-token");
}
