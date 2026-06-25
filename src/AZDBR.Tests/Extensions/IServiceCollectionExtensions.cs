using AZDBR.Commands.Settings;
using AZDBR.Tests.Fakes;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AZDBR.Tests.Extensions;

/// <summary>
/// Test service collection extensions.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers fake Cake services for command tests.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCakeFakes(this IServiceCollection services)
    {
        var configuration = new FakeConfiguration();
        var environment = FakeEnvironment.CreateUnixEnvironment();
        var fileSystem = new FakeFileSystem(environment);
        var globber = new Globber(fileSystem, environment);
        var log = new FakeLog();
        var context = Substitute.For<ICakeContext>();
        context.Configuration.Returns(configuration);
        context.Environment.Returns(environment);
        context.FileSystem.Returns(fileSystem);
        context.Globber.Returns(globber);
        context.Log.Returns(log);

        return services
            .AddSingleton(configuration)
            .AddSingleton(environment)
            .AddSingleton(fileSystem)
            .AddSingleton<IFileSystem>(fileSystem)
            .AddSingleton(globber)
            .AddSingleton(log)
            .AddSingleton(environment.Runtime)
            .AddSingleton(context);
    }

    /// <summary>
    /// Registers AZDBR services with a fake SQL executor.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configureFake">Optional fake executor configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzdbrTestServices(
        this IServiceCollection services,
        Action<FakeSqlExecutor>? configureFake = null)
    {
        var fakeSqlExecutor = new FakeSqlExecutor();
        configureFake?.Invoke(fakeSqlExecutor);

        return services
            .AddSingleton<IAzureSqlTokenProvider>(_ => new FakeTokenProvider())
            .AddSingleton<IAzureSqlConnectionFactory>(_ => Substitute.For<IAzureSqlConnectionFactory>())
            .AddSingleton<ISqlExecutor>(fakeSqlExecutor)
            .AddSingleton(fakeSqlExecutor)
            .AddSingleton<RefreshPreflightService>()
            .AddSingleton<IRefreshOrchestrator, RefreshOrchestrator>();
    }
}
