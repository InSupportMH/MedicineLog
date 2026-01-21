using MedicineLog.Application.Terminals;
using MedicineLog.Areas.Kiosk.Models;
using MedicineLog.Data;
using MedicineLog.Data.Entities;
using MedicineLog.Infrastructure.Auth;
using MedicineLog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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

        public TerminalController(AppDbContext db, ITerminalContextAccessor terminalCtxAccessor)
        {
            _db = db;
            _terminalCtxAccessor = terminalCtxAccessor;
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

            // Cookie for your TerminalSessionMiddleware (raw token, not hash)
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
        public IActionResult Register()
        {
            return View(new MedicineRegVm());
        }

        [RequirePairing]
        [HttpPost]
        public async Task<IActionResult> Register(MedicineRegVm model, CancellationToken ct)
        {
            if (model.Medicines.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Medicines), "Lägg till minst ett läkemedel.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var terminalCtx = _terminalCtxAccessor.Current!;

            var entry = new MedicineLogEntry
            {
                TerminalId = terminalCtx.TerminalId,
                SiteId = terminalCtx.SiteId,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                Items = model.Medicines.Select(m => new MedicineLogEntryItem
                {
                    MedicineName = m.MedicineName.Trim(),
                    Quantity = m.Quantity!.Value
                }).ToList()
            };

            await _db.MedicineLogEntries.AddAsync(entry);
            await _db.SaveChangesAsync(ct);

            TempData["SavedOk"] = true;
            return RedirectToAction(nameof(Register));
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorVm { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
