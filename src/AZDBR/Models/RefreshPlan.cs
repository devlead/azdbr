namespace AZDBR.Models;

/// <summary>
/// Describes the planned steps for a refresh operation.
/// </summary>
public sealed record RefreshPlan
{
    /// <summary>
    /// Gets the ordered list of steps that will be executed.
    /// </summary>
    public required IReadOnlyList<string> Steps { get; init; }

    /// <summary>
    /// Gets the source endpoint description.
    /// </summary>
    public required string SourceDescription { get; init; }

    /// <summary>
    /// Gets the target endpoint description.
    /// </summary>
    public required string TargetDescription { get; init; }

    /// <summary>
    /// Gets the old database name used during the refresh.
    /// </summary>
    public required string OldDatabaseName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the old database will be dropped after success.
    /// </summary>
    public required bool DropOldDatabaseAfterSuccess { get; init; }
}
