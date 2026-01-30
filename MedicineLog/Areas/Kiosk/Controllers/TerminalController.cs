using MedicineLog.Application.Terminals;
using MedicineLog.Areas.Kiosk.Models;
using MedicineLog.Data;
using MedicineLog.Data.Entities;
using MedicineLog.Infrastructure.Auth;
using MedicineLog.Models;
using MedicineLog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MedicineLog.Areas.Terminals.Controllers
{
    [Area("Kiosk")]
    [AllowAnonymous]
    public class TerminalController : Controller
    {
        const string TerminalRefreshCookieName = TerminalSessionMiddleware.TokenName;
        static readonly DateTimeOffset NeverExpiresAtUtc =
            new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.Zero);

        readonly AppDbContext _db;
        readonly ITerminalContextAccessor _terminalCtxAccessor;
        readonly IPhotoStoreService _photoStoreService;
        readonly ILogger<TerminalController> _logger;

        public TerminalController(AppDbContext db, ITerminalContextAccessor terminalCtxAccessor, IPhotoStoreService photoStoreService, ILogger<TerminalController> logger)
        {
            _db = db;
            _terminalCtxAccessor = terminalCtxAccessor;
            _photoStoreService = photoStoreService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Pair()
        {
            return View(new TerminalPairViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Pair(TerminalPairViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var now = DateTimeOffset.UtcNow;
            var code = (vm.Code ?? "").Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError(nameof(vm.Code), "Ange parkopplingskoden.");
                return View(vm);
            }

            // Use a transaction to avoid race conditions (two terminals trying same code).
            await using var tx = await _db.Database.BeginTransactionAsync();

            var pairing = await _db.TerminalPairingCodes
                .Include(p => p.Terminal)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (pairing is null)
            {
                ModelState.AddModelError(nameof(vm.Code), "Ogiltig parkopplingskod.");
                return View(vm);
            }

            if (pairing.UsedAt is not null)
            {
                ModelState.AddModelError(nameof(vm.Code), "Koden har redan använts.");
                return View(vm);
            }

            if (pairing.ExpiresAt <= now)
            {
                ModelState.AddModelError(nameof(vm.Code), "Koden har gått ut. Be administratören skapa en ny.");
                return View(vm);
            }

            if (!pairing.Terminal.IsActive)
            {
                ModelState.AddModelError(nameof(vm.Code), "Terminalen är inaktiverad.");
                return View(vm);
            }

            // Recommended: revoke old sessions for this terminal so re-pairing replaces them.
            var activeSessions = await _db.TerminalSessions
                .Where(s => s.TerminalId == pairing.TerminalId && s.RevokedAt == null)
                .ToListAsync();

            foreach (var s in activeSessions)
                s.RevokedAt = now;

            // Create new session
            var refreshToken = TokenHelper.GenerateRefreshToken();
            var refreshTokenHash = TokenHelper.Sha256Base64(refreshToken);

            // "Never expire" in practice:
            // Postgres DateTimeOffset maps to timestamptz; using MaxValue can be risky.
            // Pick something "far enough" instead.
            var farFuture = now.AddYears(100);

            var session = new TerminalSession
            {
                TerminalId = pairing.TerminalId,
                RefreshTokenHash = refreshTokenHash,
                CreatedAt = now,
                ExpiresAt = farFuture,
                CreatedByIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };

            _db.TerminalSessions.Add(session);

            pairing.UsedAt = now;
            pairing.UsedByIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Cookie for TerminalSessionMiddleware (raw token, not hash)
            Response.Cookies.Append(
                TerminalSessionMiddleware.TokenName,
                refreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,                 // require HTTPS (good)
                    SameSite = SameSiteMode.Strict, // or Lax if you run into issues
                    IsEssential = true,
                    Path = "/",
                    Expires = farFuture
                });

            return RedirectToAction("Register", "Terminal");
        }

        [RequirePairing]
        [HttpGet]
        public async Task<IActionResult> Register(CancellationToken ct)
        {
            //Update terminal last seen
            var terminalCtx = _terminalCtxAccessor.Current!;
            var terminal = await _db.Terminals
                .Include(t => t.Site)
                .SingleAsync(t => t.Id == terminalCtx.TerminalId);
            terminal.LastSeenAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);

            ViewBag.SiteName = terminal.Site.Name;
            ViewBag.TerminalName = terminal.Name;

            return View(new MedicineRegVm());
        }

        [RequirePairing]
        [HttpPost]
        public async Task<IActionResult> Register(MedicineRegVm model, CancellationToken ct)
        {
            var terminalCtx = _terminalCtxAccessor.Current!;
            var savedPaths = new List<string>();

            try
            {

                if (!ModelState.IsValid)
                    return BadRequest(new { ok = false, validation = ToValidation(ModelState) });

                var now = DateTimeOffset.UtcNow;

                // Store under terminal/site/date
                var photoFolderRelPath = $"/site-{terminalCtx.SiteId}/terminal-{terminalCtx.TerminalId}/{DateTime.UtcNow:yyyyMMdd}";
                var items = new List<MedicineLogEntryItem>();

                foreach (var m in model.Medicines)
                {
                    var photoRelPath = await _photoStoreService.SaveAsync(m.Photo, photoFolderRelPath, ct);
                    savedPaths.Add(photoRelPath);

                    items.Add(new MedicineLogEntryItem
                    {
                        MedicineName = m.MedicineName.Trim(),
                        Quantity = m.Quantity,
                        PhotoPath = photoRelPath,
                        PhotoContentType = m.Photo.ContentType ?? "application/octet-stream",
                        PhotoLength = m.Photo.Length
                    });
                }

                var entry = new MedicineLogEntry
                {
                    TerminalId = terminalCtx.TerminalId,
                    SiteId = terminalCtx.SiteId,
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    CreatedAt = now,
                    Items = items
                };

                await _db.MedicineLogEntries.AddAsync(entry, ct);

                await _db.SaveChangesAsync(ct);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, "Failed to register medicine log entry from site {SiteId}, terminal {TerminalId}.", terminalCtx?.SiteId, terminalCtx?.TerminalId);
                // If anything fails after saving files, try to clean them up
                try
                {
                    foreach (var p in savedPaths)
                        await _photoStoreService.DeleteAsync(p, ct);
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Failed to clean up photo store after failed medicine log entry from site {SiteId}, terminal {TerminalId}.", terminalCtx?.SiteId, terminalCtx?.TerminalId);
                }

                return StatusCode(500, new
                {
                    ok = false,
                    message = "Ett fel uppstod när registreringen behandlades. Försök igen och kontakta administratören om felet kvarstår."
                });
            }
        }

        static Dictionary<string, string[]> ToValidation(ModelStateDictionary modelState)
        {
            return modelState
                .Where(kvp => kvp.Value?.Errors?.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Ogiltigt värde." : e.ErrorMessage).ToArray()
                );
        }
    }
}
