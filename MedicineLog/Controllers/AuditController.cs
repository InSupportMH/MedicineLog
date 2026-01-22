using MedicineLog.Data;
using MedicineLog.Data.Entities;
using MedicineLog.Services;
using MedicineLog.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedicineLog.Controllers;

[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Auditor}")]
public sealed class AuditController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAuditPdfuService _pdf;

    public AuditController(AppDbContext db, UserManager<AppUser> userManager, IAuditPdfuService pdf)
    {
        _db = db;
        _userManager = userManager;
        _pdf = pdf;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sites = await GetAccessibleSitesQuery()
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();

        await PopulateSitesAsync();
        return View();
    }

    private async Task PopulateSitesAsync()
    {
        var sites = await GetAccessibleSitesQuery()
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();

        ViewBag.Sites = sites
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToList();
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(AuditPdfVm model)
    {
        if (!await HasAccessToSiteAsync(model.SiteId))
            return Forbid();

        var site = await _db.Sites
            .AsNoTracking()
            .Where(s => s.Id == model.SiteId)
            .Select(s => new { s.Id, s.Name })
            .FirstOrDefaultAsync();

        if (site is null)
            return NotFound();

        var query = _db.MedicineLogEntries
            .AsNoTracking()
            .Where(e => e.SiteId == model.SiteId); // site linkage :contentReference[oaicite:2]{index=2}

        var entries = await query
            .Include(e => e.Terminal)
            .Include(e => e.Items)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new AuditPdfEntry(
                e.CreatedAt.ToLocalTime(),
                e.Terminal.Name,
                (e.FirstName + " " + e.LastName).Trim(),
                e.Items
                    .OrderBy(i => i.MedicineName)
                    .Select(i => new AuditPdfItem(i.MedicineName, i.Quantity))
                    .ToList()
            ))
            .ToListAsync();

        var data = new AuditPdfData(
            SiteName: site.Name,
            GeneratedAt: DateTime.Now,
            FromDate: null,
            ToDate: null,
            Entries: entries
        );

        var pdfBytes = _pdf.BuildSiteLogPdf(data);

        var safeSiteName = string.Concat(site.Name.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)));
        var fileName = $"Läkemedelslogg_{safeSiteName}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    private async Task<bool> HasAccessToSiteAsync(int siteId)
    {
        if (User.IsInRole(UserRoles.Admin))
            return await _db.Sites.AnyAsync(s => s.Id == siteId);

        var userId = _userManager.GetUserId(User);
        return await _db.AuditorSiteAccesses
            .AnyAsync(a => a.UserId == userId && a.SiteId == siteId);
    }

    private IQueryable<Site> GetAccessibleSitesQuery()
    {
        if (User.IsInRole(UserRoles.Admin))
            return _db.Sites.Where(s => s.IsActive);

        var userId = _userManager.GetUserId(User);
        return _db.Sites
            .Where(s => s.IsActive)
            .Where(s => s.AuditorAccess.Any(a => a.UserId == userId));
    }
}
