using MedicineLog.Models;
using MedicineLog.Data;
using MedicineLog.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin")]
    public sealed class AdminController : Controller
    {
        readonly AppDbContext _db;
        readonly UserManager<AppUser> _userManager;
        readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(AppDbContext db, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET /admin
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardVm
            {
                SiteCount = await _db.Sites.CountAsync(),
                TerminalCount = await _db.Terminals.CountAsync(),
                ActiveTerminalSessions = await _db.TerminalSessions.CountAsync(s => s.RevokedAt == null && s.ExpiresAt > DateTimeOffset.UtcNow)
            };

            return View(vm);
        }

        // -----------------------
        // Sites
        // -----------------------

        // GET /admin/sites
        [HttpGet("sites")]
        public async Task<IActionResult> Sites()
        {
            var sites = await _db.Sites
                .OrderBy(s => s.Name)
                .Select(s => new SiteListItemVm
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsActive = s.IsActive,
                    TerminalCount = s.Terminals.Count
                })
                .ToListAsync();

            return View(new SitesListVm { Sites = sites });
        }

        // GET /admin/sites/create
        [HttpGet("sites/create")]
        public IActionResult CreateSite() => View(new CreateSiteVm());

        // POST /admin/sites/create
        [ValidateAntiForgeryToken]
        [HttpPost("sites/create")]
        public async Task<IActionResult> CreateSite(CreateSiteVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var site = new Site
            {
                Name = vm.Name.Trim(),
                IsActive = vm.IsActive,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Sites.Add(site);
            await _db.SaveChangesAsync();

            TempData["Toast"] = "Plats skapad.";
            return RedirectToAction(nameof(Sites));
        }

        // GET /admin/sites/{id}/edit
        [HttpGet("sites/{id:int}/edit")]
        public async Task<IActionResult> EditSite(int id)
        {
            var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == id);
            if (site is null) return NotFound();

            var vm = new EditSiteVm
            {
                Id = site.Id,
                Name = site.Name,
                IsActive = site.IsActive
            };

            return View(vm);
        }

        // POST /admin/sites/{id}/edit
        [ValidateAntiForgeryToken]
        [HttpPost("sites/{id:int}/edit")]
        public async Task<IActionResult> EditSite(int id, EditSiteVm vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vm);

            var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == id);
            if (site is null) return NotFound();

            site.Name = vm.Name.Trim();
            site.IsActive = vm.IsActive;

            await _db.SaveChangesAsync();

            TempData["Toast"] = "Plats uppdaterad.";
            return RedirectToAction(nameof(Sites));
        }

        // -----------------------
        // Terminals
        // -----------------------

        // GET /admin/sites/{siteId}/terminals
        [HttpGet("sites/{siteId:int}/terminals")]
        public async Task<IActionResult> Terminals(int siteId)
        {
            var site = await _db.Sites
                .Include(s => s.Terminals)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site is null) return NotFound();

            var terminals = site.Terminals
                .OrderBy(t => t.Name)
                .Select(t => new TerminalListItemVm
                {
                    Id = t.Id,
                    Name = t.Name,
                    IsActive = t.IsActive,
                    LastSeenAt = t.LastSeenAt
                })
                .ToList();

            return View(new TerminalsVm
            {
                SiteId = site.Id,
                SiteName = site.Name,
                Terminals = terminals
            });
        }

        // GET /admin/sites/{siteId}/terminals/create
        [HttpGet("sites/{siteId:int}/terminals/create")]
        public async Task<IActionResult> CreateTerminal(int siteId)
        {
            var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == siteId);
            if (site is null) return NotFound();

            return View(new CreateTerminalVm { SiteId = siteId, SiteName = site.Name });
        }

        // POST /admin/sites/{siteId}/terminals/create
        [ValidateAntiForgeryToken]
        [HttpPost("sites/{siteId:int}/terminals/create")]
        public async Task<IActionResult> CreateTerminal(int siteId, CreateTerminalVm vm)
        {
            if (siteId != vm.SiteId) return BadRequest();

            var siteExists = await _db.Sites.AnyAsync(s => s.Id == siteId);
            if (!siteExists) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.SiteName = (await _db.Sites.Where(s => s.Id == siteId).Select(s => s.Name).FirstAsync());
                return View(vm);
            }

            var terminal = new Terminal
            {
                SiteId = siteId,
                Name = vm.Name.Trim(),
                IsActive = vm.IsActive,
                LastSeenAt = null
            };

            _db.Terminals.Add(terminal);
            await _db.SaveChangesAsync();

            TempData["Toast"] = "Terminal skapad.";
            return RedirectToAction(nameof(Terminals), new { siteId });
        }

        // POST /admin/terminals/{terminalId}/toggle
        [ValidateAntiForgeryToken]
        [HttpPost("terminals/{terminalId:int}/toggle")]
        public async Task<IActionResult> ToggleTerminal(int terminalId, [FromForm] int returnSiteId)
        {
            var terminal = await _db.Terminals.FirstOrDefaultAsync(t => t.Id == terminalId);
            if (terminal is null) return NotFound();

            terminal.IsActive = !terminal.IsActive;
            await _db.SaveChangesAsync();

            TempData["Toast"] = terminal.IsActive ? "Terminal aktiverad." : "Terminal inaktiverad.";
            return RedirectToAction(nameof(Terminals), new { siteId = returnSiteId });
        }

        // -----------------------
        // Pairing codes
        // -----------------------

        // GET /admin/terminals/{terminalId}/pair
        [HttpGet("terminals/{terminalId:int}/pair")]
        public async Task<IActionResult> PairTerminal(int terminalId)
        {
            var terminal = await _db.Terminals
                .Include(t => t.Site)
                .FirstOrDefaultAsync(t => t.Id == terminalId);

            if (terminal is null) return NotFound();

            var activeCode = await _db.TerminalPairingCodes
                .Where(c => c.TerminalId == terminalId && c.UsedAt == null && c.ExpiresAt > DateTimeOffset.UtcNow)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            return View(new PairTerminalVm
            {
                TerminalId = terminal.Id,
                TerminalName = terminal.Name,
                SiteId = terminal.SiteId,
                SiteName = terminal.Site.Name,
                ExistingCode = activeCode?.Code,
                ExistingExpiresAt = activeCode?.ExpiresAt
            });
        }

        // POST /admin/terminals/{terminalId}/pair
        [ValidateAntiForgeryToken]
        [HttpPost("terminals/{terminalId:int}/pair")]
        public async Task<IActionResult> IssuePairingCode(int terminalId, [FromForm] int minutesValid = 10)
        {
            minutesValid = Math.Clamp(minutesValid, 1, 60);

            var terminal = await _db.Terminals.FirstOrDefaultAsync(t => t.Id == terminalId);
            if (terminal is null) return NotFound();

            // Invalidate old unused codes (optional policy)
            var oldCodes = await _db.TerminalPairingCodes
                .Where(c => c.TerminalId == terminalId && c.UsedAt == null && c.ExpiresAt > DateTimeOffset.UtcNow)
                .ToListAsync();

            foreach (var c in oldCodes)
                c.ExpiresAt = DateTimeOffset.UtcNow; // expire immediately

            var code = GenerateHumanCode(6); // e.g. "A7K2Q9"

            _db.TerminalPairingCodes.Add(new TerminalPairingCode
            {
                TerminalId = terminalId,
                Code = code,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(minutesValid),
                UsedAt = null,
                UsedByIpAddress = null
            });

            await _db.SaveChangesAsync();

            TempData["Toast"] = "Ny parkod skapad.";
            return RedirectToAction(nameof(PairTerminal), new { terminalId });
        }

        private static string GenerateHumanCode(int length)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // avoid I,O,1,0
            Span<char> buf = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                buf[i] = alphabet[Random.Shared.Next(alphabet.Length)];
            }
            return new string(buf);
        }

        // -----------------------
        // Auditor access
        // -----------------------

        // GET /admin/auditors
        [HttpGet("auditors")]
        public async Task<IActionResult> Auditors()
        {
            var sites = await _db.Sites
                .OrderBy(s => s.Name)
                .Select(s => new SiteOptionVm { Id = s.Id, Name = s.Name })
                .ToListAsync();

            // Show recent grants
            var grants = await _db.AuditorSiteAccesses
                .Include(a => a.User)
                .Include(a => a.Site)
                .OrderByDescending(a => a.GrantedAt)
                .Take(100)
                .Select(a => new AuditorGrantListItemVm
                {
                    UserEmail = a.User.Email!,
                    SiteId = a.SiteId,
                    SiteName = a.Site.Name,
                    GrantedAt = a.GrantedAt
                })
                .ToListAsync();

            return View(new AuditorsVm
            {
                Sites = sites,
                RecentGrants = grants,
                Grant = new GrantAuditorAccessVm()
            });
        }

        // POST /admin/auditors/grant
        [ValidateAntiForgeryToken]
        [HttpPost("auditors/grant")]
        public async Task<IActionResult> GrantAuditorAccess(GrantAuditorAccessVm vm)
        {
            if (!ModelState.IsValid)
                return await Auditors(); // simple fallback: re-render with lists

            var user = await _userManager.FindByEmailAsync(vm.Email.Trim());
            if (user is null)
            {
                TempData["Toast"] = "Ingen användare hittades med den e-postadressen.";
                return RedirectToAction(nameof(Auditors));
            }

            var siteExists = await _db.Sites.AnyAsync(s => s.Id == vm.SiteId);
            if (!siteExists)
            {
                TempData["Toast"] = "Platsen finns inte.";
                return RedirectToAction(nameof(Auditors));
            }

            var already = await _db.AuditorSiteAccesses.AnyAsync(a => a.UserId == user.Id && a.SiteId == vm.SiteId);
            if (already)
            {
                TempData["Toast"] = "Åtkomst finns redan.";
                return RedirectToAction(nameof(Auditors));
            }

            _db.AuditorSiteAccesses.Add(new AuditorSiteAccess
            {
                UserId = user.Id,
                SiteId = vm.SiteId,
                GrantedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Toast"] = "Åtkomst tilldelad.";
            return RedirectToAction(nameof(Auditors));
        }

        // POST /admin/auditors/revoke
        [ValidateAntiForgeryToken]
        [HttpPost("auditors/revoke")]
        public async Task<IActionResult> RevokeAuditorAccess([FromForm] string email, [FromForm] int siteId)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user is null) return RedirectToAction(nameof(Auditors));

            var grant = await _db.AuditorSiteAccesses.FirstOrDefaultAsync(a => a.UserId == user.Id && a.SiteId == siteId);
            if (grant is null) return RedirectToAction(nameof(Auditors));

            _db.AuditorSiteAccesses.Remove(grant);
            await _db.SaveChangesAsync();

            TempData["Toast"] = "Åtkomst borttagen.";
            return RedirectToAction(nameof(Auditors));
        }

        // -----------------------
        // Users
        // -----------------------

        // GET /admin/users?query=...
        [HttpGet("users")]
        public async Task<IActionResult> Users([FromQuery] string? query = null)
        {
            query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

            var usersQ = _userManager.Users.AsQueryable();

            if (query is not null)
                usersQ = usersQ.Where(u => u.Email!.Contains(query) || u.UserName!.Contains(query));

            // Keep it simple (you can add paging later)
            var users = await usersQ
                .OrderBy(u => u.Email)
                .Take(500)
                .ToListAsync();

            var items = new List<UserListItemVm>(users.Count);
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                items.Add(new UserListItemVm
                {
                    UserId = u.Id,
                    Email = u.Email ?? u.UserName ?? "(okänd)",
                    Roles = roles.OrderBy(r => r).ToList(),
                    LockoutEnd = u.LockoutEnd
                });
            }

            return View(new UsersListVm
            {
                Query = query,
                Users = items
            });
        }

        // GET /admin/users/create
        [HttpGet("users/create")]
        public async Task<IActionResult> CreateUser()
        {
            await EnsureCoreRolesExistAsync();
            return View(new CreateUserVm());
        }

        // POST /admin/users/create
        [ValidateAntiForgeryToken]
        [HttpPost("users/create")]
        public async Task<IActionResult> CreateUser(CreateUserVm vm)
        {
            await EnsureCoreRolesExistAsync();

            if (!ModelState.IsValid) return View(vm);

            var email = vm.Email.Trim().ToLowerInvariant();

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                ModelState.AddModelError(nameof(vm.Email), "En användare med den e-postadressen finns redan.");
                return View(vm);
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true // your choice
            };

            var createResult = await _userManager.CreateAsync(user, vm.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                    ModelState.AddModelError("", err.Description);
                return View(vm);
            }

            // Roles
            if (vm.IsAdmin)
                await _userManager.AddToRoleAsync(user, UserRoles.Admin.ToString());
            if (vm.IsAuditor)
                await _userManager.AddToRoleAsync(user, UserRoles.Auditor.ToString());

            TempData["Toast"] = "Användare skapad.";
            return RedirectToAction(nameof(Users));
        }

        // GET /admin/users/{userId}/edit
        [HttpGet("users/{userId}/edit")]
        public async Task<IActionResult> EditUser(string userId)
        {
            await EnsureCoreRolesExistAsync();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            return View(new EditUserVm
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? "",
                IsAdmin = roles.Contains(UserRoles.Admin.ToString()),
                IsAuditor = roles.Contains(UserRoles.Auditor.ToString()),
                LockoutEnd = user.LockoutEnd
            });
        }

        // POST /admin/users/{userId}/edit
        [ValidateAntiForgeryToken]
        [HttpPost("users/{userId}/edit")]
        public async Task<IActionResult> EditUser(string userId, EditUserVm vm)
        {
            ModelState.Remove("PasswordReset.NewPassword"); // not relevant here

            await EnsureCoreRolesExistAsync();

            if (userId != vm.UserId) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            if (!ModelState.IsValid) return View(vm);

            // Update roles
            var roles = await _userManager.GetRolesAsync(user);

            await SetRoleAsync(user, UserRoles.Admin.ToString(), vm.IsAdmin, roles);
            await SetRoleAsync(user, UserRoles.Auditor.ToString(), vm.IsAuditor, roles);

            TempData["Toast"] = "Användare uppdaterad.";
            return RedirectToAction(nameof(EditUser), new { userId });
        }

        private async Task SetRoleAsync(AppUser user, string role, bool shouldHave, IList<string> currentRoles)
        {
            var has = currentRoles.Contains(role);
            if (shouldHave && !has)
                await _userManager.AddToRoleAsync(user, role);
            else if (!shouldHave && has)
                await _userManager.RemoveFromRoleAsync(user, role);
        }

        // POST /admin/users/{userId}/lock-toggle
        [ValidateAntiForgeryToken]
        [HttpPost("users/{userId}/lock-toggle")]
        public async Task<IActionResult> ToggleLock(string userId, [FromForm] string? returnTo = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            // Ensure lockout is enabled
            await _userManager.SetLockoutEnabledAsync(user, true);

            if (isLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Toast"] = "Kontot är upplåst.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["Toast"] = "Kontot är låst.";
            }

            if (!string.IsNullOrWhiteSpace(returnTo))
                return Redirect(returnTo);

            return RedirectToAction(nameof(EditUser), new { userId });
        }

        // POST /admin/users/{userId}/password-reset
        [ValidateAntiForgeryToken]
        [HttpPost("users/{userId}/password-reset")]
        public async Task<IActionResult> ResetPassword(string userId, ResetPasswordVm vm)
        {
            if (userId != vm.UserId) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            if (!ModelState.IsValid)
            {
                // Re-render Edit view with current data
                var roles = await _userManager.GetRolesAsync(user);
                return View("EditUser", new EditUserVm
                {
                    UserId = user.Id,
                    Email = user.Email ?? user.UserName ?? "",
                    IsAdmin = roles.Contains(UserRoles.Admin.ToString()),
                    IsAuditor = roles.Contains(UserRoles.Auditor.ToString()),
                    LockoutEnd = user.LockoutEnd,
                    PasswordReset = vm
                });
            }

            // Identity requires a reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                var roles = await _userManager.GetRolesAsync(user);
                return View("EditUser", new EditUserVm
                {
                    UserId = user.Id,
                    Email = user.Email ?? user.UserName ?? "",
                    IsAdmin = roles.Contains(UserRoles.Admin.ToString()),
                    IsAuditor = roles.Contains(UserRoles.Auditor.ToString()),
                    LockoutEnd = user.LockoutEnd,
                    PasswordReset = vm
                });
            }

            TempData["Toast"] = "Lösenordet är uppdaterat.";
            return RedirectToAction(nameof(EditUser), new { userId });
        }

        // -----------------------
        // Auditor access (sites)
        // -----------------------

        // GET /admin/users/{userId}/sites
        [HttpGet("users/{userId}/sites")]
        public async Task<IActionResult> UserSites(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            var allSites = await _db.Sites
                .OrderBy(s => s.Name)
                .Select(s => new SiteOptionVm { Id = s.Id, Name = s.Name })
                .ToListAsync();

            var grantedSiteIds = await _db.AuditorSiteAccesses
                .Where(a => a.UserId == userId)
                .Select(a => a.SiteId)
                .ToListAsync();

            return View(new UserSitesVm
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? "",
                Sites = allSites,
                SelectedSiteIds = grantedSiteIds
            });
        }

        // POST /admin/users/{userId}/sites
        [ValidateAntiForgeryToken]
        [HttpPost("users/{userId}/sites")]
        public async Task<IActionResult> UserSites(string userId, UserSitesVm vm)
        {
            if (userId != vm.UserId) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            var selected = (vm.SelectedSiteIds ?? new List<int>()).Distinct().ToHashSet();

            // Existing grants
            var existing = await _db.AuditorSiteAccesses
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var existingIds = existing.Select(e => e.SiteId).ToHashSet();

            // Remove unchecked
            var toRemove = existing.Where(e => !selected.Contains(e.SiteId)).ToList();
            if (toRemove.Count > 0)
                _db.AuditorSiteAccesses.RemoveRange(toRemove);

            // Add newly checked
            var toAddIds = selected.Where(id => !existingIds.Contains(id)).ToList();
            foreach (var siteId in toAddIds)
            {
                _db.AuditorSiteAccesses.Add(new AuditorSiteAccess
                {
                    UserId = userId,
                    SiteId = siteId,
                    GrantedAt = DateTimeOffset.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            // Optional: ensure they are an Auditor if they have any sites
            var hasAnySitesNow = selected.Count > 0;
            var roles = await _userManager.GetRolesAsync(user);
            if (hasAnySitesNow && !roles.Contains(UserRoles.Auditor.ToString()))
                await _userManager.AddToRoleAsync(user, UserRoles.Auditor.ToString());
            if (!hasAnySitesNow && roles.Contains(UserRoles.Auditor.ToString()))
                await _userManager.RemoveFromRoleAsync(user, UserRoles.Auditor.ToString());

            TempData["Toast"] = "Åtkomst uppdaterad.";
            return RedirectToAction(nameof(UserSites), new { userId });
        }

        // Create roles if missing
        private async Task EnsureCoreRolesExistAsync()
        {
            if (!await _roleManager.RoleExistsAsync(UserRoles.Admin.ToString()))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin.ToString()));

            if (!await _roleManager.RoleExistsAsync(UserRoles.Auditor.ToString()))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Auditor.ToString()));
        }
    }
}

    