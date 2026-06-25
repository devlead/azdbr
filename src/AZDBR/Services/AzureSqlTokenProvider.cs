using Azure.Core;
using Azure.Identity;

namespace AZDBR.Services;

/// <summary>
/// Acquires Azure SQL access tokens using <see cref="DefaultAzureCredential"/>.
/// </summary>
public sealed class AzureSqlTokenProvider(ILogger<AzureSqlTokenProvider> logger) : IAzureSqlTokenProvider
{
    private const string AzureSqlResourceScope = "https://database.windows.net/.default";

    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(string? tenantId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Acquiring Azure SQL access token.");

        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            credentialOptions.TenantId = tenantId;
        }

        var credential = new DefaultAzureCredential(credentialOptions);
        var token = await credential.GetTokenAsync(
            new TokenRequestContext([AzureSqlResourceScope], tenantId: tenantId),
            cancellationToken);

        return token.Token;
    }
}
