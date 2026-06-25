namespace AZDBR.Services;

/// <summary>
/// Executes the staged refresh pipeline with progress reporting and recovery handling.
/// </summary>
public sealed class RefreshOrchestrator(
    RefreshPreflightService preflightService,
    StagingPrincipalSyncService stagingPrincipalSyncService,
    ISqlExecutor sqlExecutor,
    IAzureSqlTokenProvider tokenProvider,
    ILogger<RefreshOrchestrator> logger) : IRefreshOrchestrator
{
    /// <inheritdoc />
    public async Task ExecuteAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var plan = await preflightService.ValidateAndBuildPlanAsync(request, cancellationToken);
        PrintPlan(plan);

        if (request.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry run complete. No changes were made.[/]");
            return;
        }

        if (!request.SkipConfirmation && !ConfirmExecution(plan))
        {
            throw new InvalidOperationException("Refresh cancelled by user.");
        }

        await tokenProvider.GetAccessTokenAsync(request.TenantId, cancellationToken);

        try
        {
            await RunStepAsync("Kill active sessions", async () =>
            {
                await ExecuteSqlAsync(
                    request,
                    request.TargetServer,
                    request.TargetDatabase,
                    SqlScriptBuilder.BuildKillSessionsScript(),
                    cancellationToken);
            });

            await RunStepAsync("Rename target database", async () =>
            {
                await ExecuteSqlAsync(
                    request,
                    request.TargetServer,
                    "master",
                    SqlScriptBuilder.BuildRenameDatabaseScript(request.TargetDatabase, request.OldDatabaseName),
                    cancellationToken);
            });

            await RunStepAsync("Verify target name is free", async () =>
            {
                await preflightService.VerifyTargetNameIsFreeAsync(request, cancellationToken);
            });

            await CopyDatabaseAsync(request, cancellationToken);

            await RunStepAsync("Fix orphaned users", async () =>
            {
                await ExecuteSqlAsync(
                    request,
                    request.TargetServer,
                    request.TargetDatabase,
                    SqlScriptBuilder.BuildFixOrphanedUsersScript(),
                    cancellationToken);
            });

            await RunStepAsync("Sync staging-only principals", async () =>
            {
                await stagingPrincipalSyncService.SyncAsync(request, cancellationToken);
            });

            if (!request.KeepOldDatabase)
            {
                await RunStepAsync("Drop old database", async () =>
                {
                    await ExecuteSqlAsync(
                        request,
                        request.TargetServer,
                        "master",
                        SqlScriptBuilder.BuildDropDatabaseScript(request.OldDatabaseName),
                        cancellationToken);
                });
            }

            AnsiConsole.MarkupLine("[green]Refresh completed successfully.[/]");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Refresh failed.");
            await HandleFailureAsync(request, exception, cancellationToken);
            throw;
        }
    }

    private static void PrintPlan(RefreshPlan plan)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Setting");
        table.AddColumn("Value");
        table.AddRow("Source", plan.SourceDescription);
        table.AddRow("Target", plan.TargetDescription);
        table.AddRow("Old database", plan.OldDatabaseName);
        table.AddRow("Drop old after success", plan.DropOldDatabaseAfterSuccess ? "Yes" : "No");
        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Planned steps[/]");
        foreach (var step in plan.Steps)
        {
            AnsiConsole.MarkupLine($"  [grey]-[/] {Markup.Escape(step)}");
        }

        AnsiConsole.WriteLine();
    }

    private static bool ConfirmExecution(RefreshPlan plan)
    {
        return AnsiConsole.Confirm(
            $"Proceed with refresh of [bold]{Markup.Escape(plan.TargetDescription)}[/] from [bold]{Markup.Escape(plan.SourceDescription)}[/]?",
            false);
    }

    private static async Task RunStepAsync(string title, Func<Task> action)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"[cyan]{Markup.Escape(title)}[/]...", async _ => await action());
    }

    private async Task CopyDatabaseAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var copySql = SqlScriptBuilder.BuildCreateCopyScript(
            request.TargetDatabase,
            request.SourceServer,
            request.SourceDatabase,
            request.ServiceObjective,
            request.BackupStorageRedundancy);

        using var copyCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        copyCancellation.CancelAfter(request.CopyTimeout);

        var copyToken = copyCancellation.Token;

        await AnsiConsole.Progress()
            .Columns(
                new SpinnerColumn(),
                new TaskDescriptionColumn(),
                new ProgressBarColumn
                {
                    CompletedStyle = new Style(Color.Green),
                    FinishedStyle = new Style(Color.Lime),
                    RemainingStyle = new Style(Color.Grey)
                },
                new PercentageColumn(),
                new ElapsedTimeColumn())
            .StartAsync(async progressContext =>
            {
                var copyDescription = $"Copying {request.TargetDatabase}";
                var progressTask = progressContext.AddTask(copyDescription, maxValue: 100);

                var createTask = sqlExecutor.ExecuteNonQueryAsync(
                    request.TargetServer,
                    "master",
                    request.TenantId,
                    copySql,
                    copyToken,
                    request.CopyTimeout);

                while (true)
                {
                    copyToken.ThrowIfCancellationRequested();

                    if (createTask.IsCompleted)
                    {
                        await createTask;
                    }

                    var state = await sqlExecutor.GetDatabaseStateAsync(
                        request.TargetServer,
                        request.TargetDatabase,
                        request.TenantId,
                        copyToken);

                    if (string.Equals(state, "SUSPECT", StringComparison.OrdinalIgnoreCase))
                    {
                        await ExecuteSqlAsync(
                            request,
                            request.TargetServer,
                            "master",
                            SqlScriptBuilder.BuildDropDatabaseScript(request.TargetDatabase),
                            cancellationToken);

                        throw new InvalidOperationException(
                            $"Database copy failed and target database '{request.TargetDatabase}' was dropped because it entered SUSPECT state.");
                    }

                    if (string.Equals(state, "ONLINE", StringComparison.OrdinalIgnoreCase))
                    {
                        progressTask.Value = 100;
                        progressTask.Description = $"Copy complete ({request.TargetDatabase})";
                        break;
                    }

                    var percent = await sqlExecutor.GetCopyPercentCompleteAsync(
                        request.TargetServer,
                        request.TargetDatabase,
                        request.TenantId,
                        copyToken);

                    var stateLabel = state ?? "COPYING";
                    progressTask.Description = $"{copyDescription} ({stateLabel})";

                    if (percent.HasValue)
                    {
                        progressTask.IsIndeterminate = false;
                        progressTask.Value = percent.Value;
                    }
                    else
                    {
                        progressTask.IsIndeterminate = true;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), copyToken);
                }

                await createTask;
            });
    }

    private Task<int> ExecuteSqlAsync(
        RefreshRequest request,
        string serverName,
        string databaseName,
        string sql,
        CancellationToken cancellationToken,
        TimeSpan? commandTimeout = null) =>
        sqlExecutor.ExecuteNonQueryAsync(
            serverName,
            databaseName,
            request.TenantId,
            sql,
            cancellationToken,
            commandTimeout ?? request.CommandTimeout);

    private async Task HandleFailureAsync(
        RefreshRequest request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[red]Refresh failed.[/]");
        AnsiConsole.WriteException(exception);

        var newDbExists = await sqlExecutor.DatabaseExistsAsync(
            request.TargetServer,
            request.TargetDatabase,
            request.TenantId,
            cancellationToken);

        var oldDbExists = await sqlExecutor.DatabaseExistsAsync(
            request.TargetServer,
            request.OldDatabaseName,
            request.TenantId,
            cancellationToken);

        AnsiConsole.MarkupLine("[yellow]Recovery hints:[/]");
        if (newDbExists && oldDbExists)
        {
            AnsiConsole.MarkupLine(
                $"  - Both '{request.TargetDatabase}' and '{request.OldDatabaseName}' exist. Review manually before dropping either database.");
        }
        else if (!newDbExists && oldDbExists)
        {
            AnsiConsole.MarkupLine(
                $"  - Rename '{request.OldDatabaseName}' back to '{request.TargetDatabase}' if you need to restore the previous staging database.");
        }
        else if (newDbExists && !oldDbExists)
        {
            AnsiConsole.MarkupLine(
                $"  - New database '{request.TargetDatabase}' exists. Review its state before retrying.");
        }
    }
}
