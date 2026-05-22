using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecurityRule.Domain.Interfaces;

namespace SecurityRule.Infrastructure.Services;

/// <summary>
/// Production implementation of <see cref="IAdService"/> that queries a real
/// Active Directory via <see cref="System.DirectoryServices.AccountManagement"/>.
/// </summary>
/// <remarks>
/// Configuration keys (all under the <c>ActiveDirectory</c> section):
/// <list type="bullet">
///   <item><c>Domain</c> – FQDN or NetBIOS name of the AD domain (required).</item>
///   <item><c>LdapPath</c> – Optional LDAP path (e.g. <c>DC=corp,DC=example,DC=com</c>).</item>
///   <item><c>UserName</c> – Optional service-account name for non-Windows authentication.</item>
///   <item><c>Password</c> – Optional service-account password.</item>
/// </list>
///
/// This class is Windows-only because <see cref="System.DirectoryServices.AccountManagement"/>
/// only supports Windows. Register it conditionally in <c>Program.cs</c>:
/// <code>
/// if (OperatingSystem.IsWindows())
///     builder.Services.AddScoped&lt;IAdService, ActiveDirectoryService&gt;();
/// </code>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class ActiveDirectoryService : IAdService
{
    private readonly string _domain;
    private readonly string? _ldapPath;
    private readonly string? _userName;
    private readonly string? _password;
    private readonly ILogger<ActiveDirectoryService> _logger;

    public ActiveDirectoryService(
        IConfiguration configuration,
        ILogger<ActiveDirectoryService> logger)
    {
        _logger = logger;
        var section = configuration.GetSection("ActiveDirectory");
        _domain   = section["Domain"]   ?? string.Empty;
        _ldapPath = section["LdapPath"];
        _userName = section["UserName"];
        _password = section["Password"];
    }

    // ── IAdService ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetUserGroupNamesAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var ctx = CreatePrincipalContext();
                using var user = UserPrincipal.FindByIdentity(
                    ctx, IdentityType.SamAccountName, userName);

                if (user == null)
                {
                    _logger.LogWarning("AD user '{UserName}' not found.", userName);
                    return [];
                }

                using var groups = user.GetGroups();
                return groups.Select(g => g.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying AD groups for user '{UserName}'.", userName);
                return [];
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetGroupMemberUserNamesAsync(string groupName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var ctx = CreatePrincipalContext();
                using var group = GroupPrincipal.FindByIdentity(
                    ctx, IdentityType.Name, groupName);

                if (group == null)
                {
                    _logger.LogWarning("AD group '{GroupName}' not found.", groupName);
                    return [];
                }

                using var members = group.GetMembers(recursive: false);
                return members
                    .OfType<UserPrincipal>()
                    .Select(u => u.SamAccountName ?? u.Name ?? string.Empty)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying AD members for group '{GroupName}'.", groupName);
                return [];
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetGroupChildGroupNamesAsync(string groupName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var ctx = CreatePrincipalContext();
                using var group = GroupPrincipal.FindByIdentity(
                    ctx, IdentityType.Name, groupName);

                if (group == null)
                {
                    _logger.LogWarning("AD group '{GroupName}' not found.", groupName);
                    return [];
                }

                using var members = group.GetMembers(recursive: false);
                return members
                    .OfType<GroupPrincipal>()
                    .Select(g => g.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error querying AD child groups for group '{GroupName}'.", groupName);
                return [];
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetGroupParentGroupNamesAsync(string groupName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var ctx = CreatePrincipalContext();
                using var group = GroupPrincipal.FindByIdentity(
                    ctx, IdentityType.Name, groupName);

                if (group == null)
                {
                    _logger.LogWarning("AD group '{GroupName}' not found.", groupName);
                    return [];
                }

                using var parentGroups = group.GetGroups();
                return parentGroups.Select(g => g.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error querying AD parent groups for group '{GroupName}'.", groupName);
                return [];
            }
        }, cancellationToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private PrincipalContext CreatePrincipalContext()
    {
        var contextType = string.IsNullOrEmpty(_ldapPath)
            ? ContextType.Domain
            : ContextType.Domain;

        if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
        {
            return new PrincipalContext(
                contextType,
                _domain,
                _ldapPath,
                _userName,
                _password);
        }

        return string.IsNullOrEmpty(_ldapPath)
            ? new PrincipalContext(contextType, _domain)
            : new PrincipalContext(contextType, _domain, _ldapPath);
    }
}
