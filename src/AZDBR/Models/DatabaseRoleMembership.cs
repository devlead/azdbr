namespace AZDBR.Models;

/// <summary>
/// Represents a database role membership.
/// </summary>
/// <param name="RoleName">Role name.</param>
/// <param name="MemberName">Member principal name.</param>
public sealed record DatabaseRoleMembership(string RoleName, string MemberName);
