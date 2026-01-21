namespace MedicineLog.Services;

public interface IAuditPdfuService
{
    byte[] BuildSiteLogPdf(AuditPdfData data);
}

public sealed record AuditPdfData(
    string SiteName,
    DateTimeOffset GeneratedAt,
    DateTime? FromDate,
    DateTime? ToDate,
    IReadOnlyList<AuditPdfEntry> Entries
);

public sealed record AuditPdfEntry(
    DateTimeOffset CreatedAt,
    string TerminalName,
    string StaffName,
    IReadOnlyList<AuditPdfItem> Items
);

public sealed record AuditPdfItem(string MedicineName, int Quantity);
