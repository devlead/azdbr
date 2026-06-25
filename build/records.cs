/*****************************
 * Records
 *****************************/
public record BuildData(
    string Version,
    bool IsMainBranch,
    bool ShouldNotPublish,
    bool IsLocalBuild,
    DirectoryPath ProjectRoot,
    FilePath ProjectPath,
    DotNetMSBuildSettings MSBuildSettings,
    DirectoryPath ArtifactsPath,
    DirectoryPath OutputPath)
{
    private const string IntegrationTest = "integrationtest";

    public DirectoryPath NuGetOutputPath { get; } = OutputPath.Combine("nuget");

    public DirectoryPath IntegrationTestPath { get; } = OutputPath.Combine(IntegrationTest);

    public string? GitHubNuGetSource { get; } = System.Environment.GetEnvironmentVariable("GH_PACKAGES_NUGET_SOURCE");
    public string? GitHubNuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("GITHUB_TOKEN");

    public bool ShouldPushGitHubPackages() => !ShouldNotPublish
        && !string.IsNullOrWhiteSpace(GitHubNuGetSource)
        && !string.IsNullOrWhiteSpace(GitHubNuGetApiKey);

    public string? NuGetSource { get; } = System.Environment.GetEnvironmentVariable("NUGET_SOURCE");
    public string? NuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");

    public bool ShouldPushNuGetPackages() => IsMainBranch
        && !ShouldNotPublish
        && !string.IsNullOrWhiteSpace(NuGetSource)
        && !string.IsNullOrWhiteSpace(NuGetApiKey);

    public string? IntegrationSourceServer { get; } = System.Environment.GetEnvironmentVariable("AZDBR_INTEGRATION_SOURCE_SERVER");
    public string? IntegrationSourceDatabase { get; } = System.Environment.GetEnvironmentVariable("AZDBR_INTEGRATION_SOURCE_DATABASE");
    public string? IntegrationTargetServer { get; } = System.Environment.GetEnvironmentVariable("AZDBR_INTEGRATION_TARGET_SERVER");
    public string? IntegrationTargetDatabase { get; } = System.Environment.GetEnvironmentVariable("AZDBR_INTEGRATION_TARGET_DATABASE");
    public string? IntegrationTenantId { get; } = System.Environment.GetEnvironmentVariable("AZDBR_INTEGRATION_TENANT_ID");

    public AzureCredentials AzureCredentials { get; } = new AzureCredentials(
        System.Environment.GetEnvironmentVariable("AZURE_TENANT_ID"),
        System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"),
        System.Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"),
        System.Environment.GetEnvironmentVariable("AZURE_AUTHORITY_HOST"));

    public bool IntegrationRefreshConfigured =>
        !string.IsNullOrWhiteSpace(IntegrationSourceServer)
        && !string.IsNullOrWhiteSpace(IntegrationSourceDatabase)
        && !string.IsNullOrWhiteSpace(IntegrationTargetServer)
        && !string.IsNullOrWhiteSpace(IntegrationTargetDatabase);

    public bool ShouldRunAzureIntegrationTests() =>
        IntegrationRefreshConfigured
        && (IsLocalBuild || AzureCredentials.AzureCredentialsSpecified);

    public ICollection<DirectoryPath> DirectoryPathsToClean { get; } =
    [
        ArtifactsPath,
        OutputPath,
        OutputPath.Combine(IntegrationTest)
    ];
}

public record AzureCredentials(
    string? TenantId,
    string? ClientId,
    string? ClientSecret,
    string? AuthorityHost = "https://login.microsoftonline.com")
{
    public bool AzureCredentialsSpecified { get; } = !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(AuthorityHost);
}

internal record ExtensionHelper(Func<string, CakeTaskBuilder> TaskCreate, Func<CakeReport> Run);
