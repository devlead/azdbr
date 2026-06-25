namespace AZDBR.Extensions;

/// <summary>
/// Dependency injection extensions for AZDBR services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AZDBR domain services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzdbrServices(this IServiceCollection services) =>
        services
            .AddSingleton<IAzureSqlTokenProvider, AzureSqlTokenProvider>()
            .AddSingleton<IAzureSqlConnectionFactory, AzureSqlConnectionFactory>()
            .AddSingleton<ISqlExecutor, SqlExecutor>()
            .AddSingleton<RefreshPreflightService>()
            .AddSingleton<StagingPrincipalSyncService>()
            .AddSingleton<IRefreshOrchestrator, RefreshOrchestrator>();
}
