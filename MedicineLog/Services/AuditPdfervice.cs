using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedicineLog.Services;

public sealed class AuditPdfService : IAuditPdfuService
{
    string _rootPhotoPath;

    public AuditPdfService(IConfiguration cfg)
    {
        _rootPhotoPath = cfg["PhotoStore:Root"] ?? throw new InvalidOperationException("PhotoStore:Root missing");
    }
    public byte[] BuildSiteLogPdf(AuditPdfData data)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Registrerade läkemedel").FontSize(18).SemiBold();
                    col.Item().Text($"Plats: {data.SiteName}").FontSize(12);
                    col.Item().Text($"Rapport skapad: {data.GeneratedAt:yyyy-MM-dd HH:mm}");
                    if (data.FromDate is not null || data.ToDate is not null)
                    {
                        var from = data.FromDate?.ToString("yyyy-MM-dd") ?? "-";
                        var to = data.ToDate?.ToString("yyyy-MM-dd") ?? "-";
                        col.Item().Text($"Period: {from} till {to}");
                    }
                    col.Item().LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    if (data.Entries.Count == 0)
                    {
                        col.Item().PaddingTop(10).Text("Inga loggposter hittades.");
                        return;
                    }

                    foreach (var entry in data.Entries)
                    {
                        col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(8).Column(e =>
                        {
                            e.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{entry.CreatedAt:yyyy-MM-dd HH:mm}").SemiBold();
                                r.RelativeItem().AlignRight().Text($"Personal: {entry.StaffName}").SemiBold();
                            });

                            e.Item().PaddingTop(6).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(4);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(2);
                                });

                                t.Header(h =>
                                {
                                    h.Cell().Text("Läkemedel").SemiBold();
                                    h.Cell().AlignRight().Text("Antal").SemiBold();
                                    h.Cell().AlignRight().Text("Bild").SemiBold();
                                    h.Cell().ColumnSpan(3).PaddingTop(2).LineHorizontal(0.5f);
                                });

                                foreach (var item in entry.Items)
                                {
                                    t.Cell().Text(item.MedicineName);
                                    t.Cell().AlignRight().Text(item.Quantity.ToString());

                                        var absPhotoPath = Path.Combine(_rootPhotoPath, item.PhotoPath);
                                        if (File.Exists(absPhotoPath))
                                            t.Cell().Image(absPhotoPath);
                                        else
                                            t.Cell().AlignRight().Text("[Bild saknas]").Italic().FontColor(Colors.Red.Medium);
                                }
                            });
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Sida ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }
}
