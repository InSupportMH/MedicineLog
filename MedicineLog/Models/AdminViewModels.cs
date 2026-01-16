using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Models;

public sealed class AdminDashboardVm
{
    public int SiteCount { get; set; }
    public int TerminalCount { get; set; }
    public int ActiveTerminalSessions { get; set; }
}

public sealed class SitesListVm
{
    public List<SiteListItemVm> Sites { get; set; } = new();
}

public sealed class SiteListItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public int TerminalCount { get; set; }
}

public class CreateSiteVm
{
    [Required(ErrorMessage = "Namn är obligatoriskt.")]
    [StringLength(200, ErrorMessage = "Max 200 tecken.")]
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

public sealed class EditSiteVm : CreateSiteVm
{
    public int Id { get; set; }
}

public sealed class TerminalsVm
{
    public int SiteId { get; set; }
    public string SiteName { get; set; } = "";
    public List<TerminalListItemVm> Terminals { get; set; } = new();
}

public sealed class TerminalListItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
}

public sealed class CreateTerminalVm
{
    public int SiteId { get; set; }
    public string SiteName { get; set; } = "";

    [Required(ErrorMessage = "Namn är obligatoriskt.")]
    [StringLength(200, ErrorMessage = "Max 200 tecken.")]
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

public sealed class PairTerminalVm
{
    public int TerminalId { get; set; }
    public string TerminalName { get; set; } = "";
    public int SiteId { get; set; }
    public string SiteName { get; set; } = "";

    public string? ExistingCode { get; set; }
    public DateTimeOffset? ExistingExpiresAt { get; set; }
}

public sealed class AuditorsVm
{
    public List<SiteOptionVm> Sites { get; set; } = new();
    public List<AuditorGrantListItemVm> RecentGrants { get; set; } = new();
    public GrantAuditorAccessVm Grant { get; set; } = new();
}

public sealed class SiteOptionVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class AuditorGrantListItemVm
{
    public string UserEmail { get; set; } = "";
    public int SiteId { get; set; }
    public string SiteName { get; set; } = "";
    public DateTimeOffset GrantedAt { get; set; }
}

public sealed class GrantAuditorAccessVm
{
    [Required(ErrorMessage = "E-post är obligatoriskt.")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
    public string Email { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Välj en plats.")]
    public int SiteId { get; set; }
}

public sealed class UsersListVm
{
    public string? Query { get; set; }
    public List<UserListItemVm> Users { get; set; } = new();
}

public sealed class UserListItemVm
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public DateTimeOffset? LockoutEnd { get; set; }
}

public sealed class CreateUserVm
{
    [Required(ErrorMessage = "E-post är obligatoriskt.")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Lösenord är obligatoriskt.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Minst 8 tecken.")]
    public string Password { get; set; } = "";

    public bool IsAdmin { get; set; }
    public bool IsAuditor { get; set; }
}

public sealed class EditUserVm
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";

    public bool IsAdmin { get; set; }
    public bool IsAuditor { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public ResetPasswordVm PasswordReset { get; set; } = new();
}

public sealed class UpdateUserRolesVm
{
    public string UserId { get; set; } = "";
    public bool IsAdmin { get; set; }
    public bool IsAuditor { get; set; }
}

public sealed class ResetPasswordVm
{
    public string UserId { get; set; } = "";

    [Required(ErrorMessage = "Nytt lösenord är obligatoriskt.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Minst 8 tecken.")]
    public string NewPassword { get; set; } = "";
}

public sealed class UserSitesVm
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";

    public List<SiteOptionVm> Sites { get; set; } = new();
    public List<int> SelectedSiteIds { get; set; } = new();
}