namespace AZDBR.Models;

/// <summary>
/// Represents the state of an Azure SQL database as reported by sys.databases.
/// </summary>
public enum DatabaseState
{
    /// <summary>
    /// Unknown or unrecognized state.
    /// </summary>
    Unknown,

    /// <summary>
    /// Database is online and available.
    /// </summary>
    Online,

    /// <summary>
    /// Database copy is in progress.
    /// </summary>
    Copying,

    /// <summary>
    /// Database is in suspect state after a failed operation.
    /// </summary>
    Suspect,

    /// <summary>
    /// Database does not exist.
    /// </summary>
    NotFound
}
