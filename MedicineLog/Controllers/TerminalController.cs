using MedicineLog.Application.Terminals;
using MedicineLog.Data;
using MedicineLog.Data.Entities;
using MedicineLog.Infrastructure.Auth;
using MedicineLog.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MedicineLog.Controllers
{
    public class TerminalController : Controller
    {
        readonly AppDbContext _db;
        readonly ITerminalContextAccessor _terminalCtxAccessor;

        public TerminalController(AppDbContext db, ITerminalContextAccessor terminalCtxAccessor)
        {
            _db = db;
            _terminalCtxAccessor = terminalCtxAccessor;
        }

        [RequireTerminal]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new MedicineRegViewModel());
        }

        [RequireTerminal]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(MedicineRegViewModel model, CancellationToken ct)
        {
            model.Medicines ??= new List<MedicineItemViewModel>();

            // Iignore fully empty rows
            var filtered = model.Medicines
                .Where(m => !string.IsNullOrWhiteSpace(m.MedicineName) || m.Quantity.HasValue)
                .ToList();

            if (filtered.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Medicines), "Lägg till minst ett läkemedel.");
            }

            if (!ModelState.IsValid)
            {
                // Keep the filtered list so the user doesn't see blank rows on error
                model.Medicines = filtered.Count > 0 ? filtered : new List<MedicineItemViewModel> { new() };
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
                Items = filtered.Select(m => new MedicineLogEntryItem
                {
                    MedicineName = m.MedicineName.Trim(),
                    Quantity = m.Quantity!.Value
                }).ToList()
            };

            _db.MedicineLogEntries.Add(entry);
            await _db.SaveChangesAsync(ct);

            return RedirectToAction(nameof(Register));
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
