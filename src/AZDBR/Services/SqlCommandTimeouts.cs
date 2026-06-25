namespace AZDBR.Services;

/// <summary>
/// Default SQL command timeout values used by the tool.
/// </summary>
internal static class SqlCommandTimeouts
{
    /// <summary>
    /// Minimum command timeout for long-running operations such as database copy.
    /// </summary>
    public static readonly TimeSpan Minimum = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Default command timeout for SQL batches.
    /// </summary>
    public static readonly TimeSpan Default = Minimum;

    /// <summary>
    /// Converts a timeout to SQL command timeout seconds.
    /// </summary>
    /// <param name="timeout">Requested timeout.</param>
    /// <returns>Timeout in seconds for <see cref="Microsoft.Data.SqlClient.SqlCommand.CommandTimeout"/>.</returns>
    public static int ToSeconds(TimeSpan timeout) =>
        Math.Max(1, (int)Math.Ceiling(timeout.TotalSeconds));
}
