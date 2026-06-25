#!/usr/bin/env dotnet
#:sdk Cake.Sdk@6.2.0
#:property IncludeAdditionalFiles=./build/*.cs

/*****************************
 * Setup
 *****************************/
Setup(
    static context =>
    {
        InstallTool("dotnet:https://api.nuget.org/v3/index.json?package=DPI&version=2026.5.18.419");
        InstallTool("dotnet:https://api.nuget.org/v3/index.json?package=GitVersion.Tool&version=6.7.0");

        var assertedVersions = context.GitVersion(new GitVersionSettings
        {
            OutputType = GitVersionOutput.Json
        });

        var branchName = assertedVersions.BranchName;
        var isMainBranch = StringComparer.OrdinalIgnoreCase.Equals("main", branchName);

        var buildDate = DateTime.UtcNow;
        var runNumber = GitHubActions.IsRunningOnGitHubActions
            ? GitHubActions.Environment.Workflow.RunNumber
            : (short)((buildDate - buildDate.Date).TotalSeconds / 3);

        var version = FormattableString.Invariant($"{buildDate:yyyy.M.d}.{runNumber}");

        context.Information("Building version {0} (Branch: {1}, IsMain: {2})",
            version,
            branchName,
            isMainBranch);

        var artifactsPath = context.MakeAbsolute(context.Directory("./artifacts"));
        var projectRoot = context.MakeAbsolute(context.Directory("./src"));
        var projectPath = projectRoot.CombineWithFilePath("AZDBR/AZDBR.csproj");

        return new BuildData(
            version,
            isMainBranch,
            !context.IsRunningOnWindows(),
            BuildSystem.IsLocalBuild,
            projectRoot,
            projectPath,
            new DotNetMSBuildSettings()
                .SetConfiguration("Release")
                .SetVersion(version)
                .WithProperty("Copyright", $"Mattias Karlsson © {DateTime.UtcNow.Year}")
                .WithProperty("Authors", "devlead")
                .WithProperty("Company", "devlead")
                .WithProperty("PackageLicenseExpression", "MIT")
                .WithProperty("PackageTags", "tool;azure;sql;database;refresh")
                .WithProperty("PackageDescription", "Azure SQL Database Refresh .NET Tool - Safely refresh staging/dev databases from production.")
                .WithProperty("RepositoryUrl", "https://github.com/devlead/azdbr.git")
                .WithProperty("ContinuousIntegrationBuild", GitHubActions.IsRunningOnGitHubActions ? "true" : "false")
                .WithProperty("EmbedUntrackedSources", "true"),
            artifactsPath,
            artifactsPath.Combine(version));
    });

/*****************************
 * Tasks
 *****************************/
Task("Clean")
    .Does<BuildData>(
        static (context, data) => context.CleanDirectories(data.DirectoryPathsToClean)
    )
.Then("Restore")
    .Does<BuildData>(
        static (context, data) => context.DotNetRestore(
            data.ProjectRoot.FullPath,
            new DotNetRestoreSettings
            {
                MSBuildSettings = data.MSBuildSettings
            })
    )
.Then("DPI")
    .Does<BuildData>(
        static (context, data) => Command(
            ["dpi", "dpi.exe"],
            new ProcessArgumentBuilder()
                .Append("nuget")
                .Append("--silent")
                .AppendSwitchQuoted("--output", "table")
                .Append(
                    (
                        !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_SharedKey"))
                        && !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_WorkspaceId"))
                    )
                        ? "report"
                        : "analyze")
                .AppendSwitchQuoted("--buildversion", data.Version))
    )
.Then("Build")
    .Does<BuildData>(
        static (context, data) => context.DotNetBuild(
            data.ProjectRoot.FullPath,
            new DotNetBuildSettings
            {
                NoRestore = true,
                MSBuildSettings = data.MSBuildSettings
            })
    )
.Then("Test")
    .Does<BuildData>(
        static (context, data) => context.DotNetTest(
            data.ProjectRoot.FullPath,
            new DotNetTestSettings
            {
                NoBuild = true,
                NoRestore = true,
                MSBuildSettings = data.MSBuildSettings
            })
    )
.Then("Pack")
    .Does<BuildData>(
        static (context, data) => context.DotNetPack(
            data.ProjectPath.FullPath,
            new DotNetPackSettings
            {
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = data.NuGetOutputPath,
                MSBuildSettings = data.MSBuildSettings
            })
    )
.Then("Upload-Artifacts")
    .WithCriteria(BuildSystem.IsRunningOnGitHubActions, nameof(BuildSystem.IsRunningOnGitHubActions))
    .Does<BuildData>(
        static (context, data) => GitHubActions
            .Commands
            .UploadArtifact(
                data.ArtifactsPath,
                $"Artifact_{GitHubActions.Environment.Runner.ImageOS ?? GitHubActions.Environment.Runner.OS}_{context.Environment.Runtime.BuiltFramework.Identifier}_{context.Environment.Runtime.BuiltFramework.Version}")
    )
.Then("Integration-Tests-Tool-Manifest")
    .Does<BuildData>(
        static (context, data) => context.DotNetTool(
            "new",
            new DotNetToolSettings
            {
                ArgumentCustomization = args => args.Append("tool-manifest"),
                WorkingDirectory = data.IntegrationTestPath
            })
    )
.Then("Integration-Tests-Tool-Install")
    .Does<BuildData>(
        static (context, data) => context.DotNetTool(
            "tool",
            new DotNetToolSettings
            {
                ArgumentCustomization = args => args
                    .Append("install")
                    .AppendSwitchQuoted("--source", data.NuGetOutputPath.FullPath)
                    .AppendSwitchQuoted("--version", data.Version)
                    .Append("azdbr"),
                WorkingDirectory = data.IntegrationTestPath
            })
    )
.Then("Integration-Tests-Tool-Help")
    .Does<BuildData>(
        static (context, data) => context.DotNetTool(
            "tool",
            new DotNetToolSettings
            {
                ArgumentCustomization = args => args
                    .Append("run")
                    .Append("--")
                    .Append("azdbr")
                    .Append("--help"),
                WorkingDirectory = data.IntegrationTestPath
            })
    )
.Then("Integration-Tests-Azure")
    .WithCriteria<BuildData>(static (context, data) => data.ShouldRunAzureIntegrationTests(), "ShouldRunAzureIntegrationTests")
    .Does<BuildData>(
        static (context, data) =>
        {
            context.Information(
                "Running Azure SQL integration refresh from {0}.{1} to {2}.{3}",
                data.IntegrationSourceServer,
                data.IntegrationSourceDatabase,
                data.IntegrationTargetServer,
                data.IntegrationTargetDatabase);

            context.DotNetTool(
                "tool",
                new DotNetToolSettings
                {
                    ArgumentCustomization = args =>
                    {
                        args
                            .Append("run")
                            .Append("--")
                            .Append("azdbr")
                            .Append("refresh")
                            .AppendQuoted(data.IntegrationSourceServer!)
                            .AppendQuoted(data.IntegrationSourceDatabase!)
                            .AppendQuoted(data.IntegrationTargetServer!)
                            .AppendQuoted(data.IntegrationTargetDatabase!)
                            .Append("-y");

                        if (!string.IsNullOrWhiteSpace(data.IntegrationTenantId))
                        {
                            args.AppendSwitchQuoted("--tenant-id", data.IntegrationTenantId);
                        }
                        
                        return args;
                    },
                    WorkingDirectory = data.IntegrationTestPath
                });
        })
.Then("Integration-Tests")
    .Default()
.Then("Push-GitHub-Packages")
    .WithCriteria<BuildData>(static (context, data) => data.ShouldPushGitHubPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context) => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context) => context.DotNetNuGetPush(
            item.FullPath,
            new DotNetNuGetPushSettings
            {
                Source = data.GitHubNuGetSource,
                ApiKey = data.GitHubNuGetApiKey
            }))
.Then("Push-NuGet-Packages")
    .WithCriteria<BuildData>(static (context, data) => data.ShouldPushNuGetPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context) => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context) => context.DotNetNuGetPush(
            item.FullPath,
            new DotNetNuGetPushSettings
            {
                Source = data.NuGetSource,
                ApiKey = data.NuGetApiKey
            }))
.Then("Create-GitHub-Release")
    .WithCriteria<BuildData>(static (context, data) => data.ShouldPushNuGetPackages())
    .Does<BuildData>(
        static (context, data) => context.Command(
            new CommandSettings
            {
                ToolName = "GitHub CLI",
                ToolExecutableNames = ["gh.exe", "gh"],
                EnvironmentVariables = { { "GH_TOKEN", data.GitHubNuGetApiKey } }
            },
            new ProcessArgumentBuilder()
                .Append("release")
                .Append("create")
                .Append(data.Version)
                .AppendSwitchQuoted("--title", data.Version)
                .Append("--generate-notes")
                .Append(string.Join(
                    ' ',
                    context
                        .GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg")
                        .Select(path => path.FullPath.Quote())))))
.Then("GitHub-Actions")
.Run();
