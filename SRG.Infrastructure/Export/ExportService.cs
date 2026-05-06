using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SRG.Application.Export;
using SRG.Domain.Entities;
using SRG.Infrastructure.Persistence;

namespace SRG.Infrastructure.Export;

public class ExportService(AppDbContext dbContext) : IExportService
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfContentType = "application/pdf";

    public async Task<ExportFileResponse> ExportDailyReportToExcelAsync(Guid dailyReportId, CancellationToken cancellationToken = default)
    {
        var dailyReport = await GetDailyReportAsync(dailyReportId, cancellationToken);
        var materialNames = await GetMaterialNamesAsync(dailyReport.MaterialUsages.Select(item => item.MaterialId), cancellationToken);

        using var workbook = new XLWorkbook();
        AddDailyReportHeaderSheet(workbook, dailyReport);
        AddHoursSheet(workbook, dailyReport);
        AddWorkSheet(workbook, dailyReport);
        AddMaterialsSheet(workbook, dailyReport, materialNames);

        return new ExportFileResponse(
            $"dailyReport-{dailyReport.Date:yyyy-MM-dd}.xlsx",
            ExcelContentType,
            SaveWorkbook(workbook));
    }

    public async Task<ExportFileResponse> ExportDailyReportToPdfAsync(Guid dailyReportId, CancellationToken cancellationToken = default)
    {
        var dailyReport = await GetDailyReportAsync(dailyReportId, cancellationToken);
        var materialNames = await GetMaterialNamesAsync(dailyReport.MaterialUsages.Select(item => item.MaterialId), cancellationToken);

        QuestPDF.Settings.License = LicenseType.Community;

        var content = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("Raport DailyReport").FontSize(22).Bold();
                    column.Item().Text($"Data: {dailyReport.Date:yyyy-MM-dd}");
                    column.Item().Text($"Crew: {dailyReport.Crew?.Name ?? "-"}");
                    column.Item().Text($"Project: {dailyReport.Project?.Name ?? "-"}");
                    column.Item().Text($"Section: {dailyReport.Section?.Name ?? "-"}");
                    column.Item().Text($"Status: {dailyReport.Status}");
                });

                page.Content().PaddingTop(20).Column(column =>
                {
                    AddPdfTable(
                        column,
                        "Godziny pracy",
                        ["Osoba", "Godziny"],
                        dailyReport.WorkHours
                            .OrderBy(entry => entry.Worker?.LastName)
                            .Select(entry => new[] { FullName(entry.Worker), entry.Hours.ToString("0.##") }));

                    AddPdfTable(
                        column,
                        "Wykonane workEntries",
                        ["Typ", "Opis", "Ilość"],
                        dailyReport.WorkEntries.Select(entry => new[]
                        {
                            entry.WorkTypeId.ToString(),
                            entry.Description ?? "-",
                            entry.Quantity.ToString("0.##")
                        }));

                    AddPdfTable(
                        column,
                        "Zużyte materiały",
                        ["Materiał", "Ilość"],
                        dailyReport.MaterialUsages.Select(entry => new[]
                        {
                            materialNames.GetValueOrDefault(entry.MaterialId, entry.MaterialId.ToString()),
                            entry.Quantity.ToString("0.##")
                        }));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Wygenerowano ");
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"));
                    text.Span(" UTC");
                });
            });
        }).GeneratePdf();

        return new ExportFileResponse($"dailyReport-{dailyReport.Date:yyyy-MM-dd}.pdf", PdfContentType, content);
    }

    public async Task<ExportFileResponse> ExportMaterialUsageToExcelAsync(
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken = default)
    {
        if (dateFrom > dateTo)
        {
            throw new ArgumentException("dateFrom cannot be later than dateTo.");
        }

        var usage = await dbContext.MaterialUsages
            .AsNoTracking()
            .Join(dbContext.DailyReports.AsNoTracking(), dailyReportMaterial => dailyReportMaterial.DailyReportId, report => report.Id, (dailyReportMaterial, report) => new { dailyReportMaterial, report })
            .Join(dbContext.Materials.AsNoTracking(), row => row.dailyReportMaterial.MaterialId, material => material.Id, (row, material) => new { row.dailyReportMaterial, row.report, material })
            .Where(row => row.report.Date >= dateFrom && row.report.Date <= dateTo)
            .GroupBy(row => new { row.dailyReportMaterial.MaterialId, row.material.Name, row.material.Unit })
            .Select(group => new
            {
                group.Key.Name,
                group.Key.Unit,
                TotalUsage = group.Sum(row => row.dailyReportMaterial.Quantity)
            })
            .OrderByDescending(row => row.TotalUsage)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Zużycie materiałów");
        sheet.Cell(1, 1).Value = "Zakres od";
        sheet.Cell(1, 2).Value = dateFrom.ToString("yyyy-MM-dd");
        sheet.Cell(2, 1).Value = "Zakres do";
        sheet.Cell(2, 2).Value = dateTo.ToString("yyyy-MM-dd");
        sheet.Cell(4, 1).Value = "Materiał";
        sheet.Cell(4, 2).Value = "Łączne zużycie";
        sheet.Cell(4, 3).Value = "Jednostka";

        for (var index = 0; index < usage.Count; index++)
        {
            var row = usage[index];
            var sheetRow = index + 5;
            sheet.Cell(sheetRow, 1).Value = row.Name;
            sheet.Cell(sheetRow, 2).Value = row.TotalUsage;
            sheet.Cell(sheetRow, 3).Value = row.Unit;
        }

        StyleWorksheet(sheet, 4, 3);

        return new ExportFileResponse(
            $"materials-{dateFrom:yyyy-MM-dd}-{dateTo:yyyy-MM-dd}.xlsx",
            ExcelContentType,
            SaveWorkbook(workbook));
    }

    public async Task<ExportFileResponse> ExportWarehouseStockToExcelAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var warehouse = await dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == warehouseId, cancellationToken)
            ?? throw new KeyNotFoundException("Warehouse not found.");

        var stock = await dbContext.WarehouseStocks
            .AsNoTracking()
            .Where(item => item.WarehouseId == warehouseId)
            .Join(dbContext.Materials.AsNoTracking(), stockItem => stockItem.MaterialId, material => material.Id, (stockItem, material) => new
            {
                material.Name,
                stockItem.Quantity,
                material.Unit
            })
            .OrderBy(row => row.Name)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Stan warehouseowy");
        sheet.Cell(1, 1).Value = "Warehouse";
        sheet.Cell(1, 2).Value = warehouse.Name;
        sheet.Cell(3, 1).Value = "Materiał";
        sheet.Cell(3, 2).Value = "Ilość";
        sheet.Cell(3, 3).Value = "Jednostka";

        for (var index = 0; index < stock.Count; index++)
        {
            var row = stock[index];
            var sheetRow = index + 4;
            sheet.Cell(sheetRow, 1).Value = row.Name;
            sheet.Cell(sheetRow, 2).Value = row.Quantity;
            sheet.Cell(sheetRow, 3).Value = row.Unit;
        }

        StyleWorksheet(sheet, 3, 3);

        return new ExportFileResponse(
            $"warehouse-{warehouse.Name}.xlsx",
            ExcelContentType,
            SaveWorkbook(workbook));
    }

    private async Task<DailyReport> GetDailyReportAsync(Guid dailyReportId, CancellationToken cancellationToken)
    {
        return await dbContext.DailyReports
            .AsNoTracking()
            .Include(report => report.Crew)
            .Include(report => report.Project)
            .Include(report => report.Section)
            .Include(report => report.WorkHours)
                .ThenInclude(entry => entry.Worker)
            .Include(report => report.WorkEntries)
            .Include(report => report.MaterialUsages)
            .FirstOrDefaultAsync(report => report.Id == dailyReportId, cancellationToken)
            ?? throw new KeyNotFoundException("DailyReport report not found.");
    }

    private async Task<Dictionary<Guid, string>> GetMaterialNamesAsync(
        IEnumerable<Guid> materialIds,
        CancellationToken cancellationToken)
    {
        var ids = materialIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.Materials
            .AsNoTracking()
            .Where(material => ids.Contains(material.Id))
            .ToDictionaryAsync(material => material.Id, material => material.Name, cancellationToken);
    }

    private static void AddDailyReportHeaderSheet(XLWorkbook workbook, DailyReport dailyReport)
    {
        var sheet = workbook.Worksheets.Add("Nagłówek");
        var rows = new (string Label, string Value)[]
        {
            ("Data", dailyReport.Date.ToString("yyyy-MM-dd")),
            ("Crew", dailyReport.Crew?.Name ?? "-"),
            ("Project", dailyReport.Project?.Name ?? "-"),
            ("Section", dailyReport.Section?.Name ?? "-"),
            ("Status", dailyReport.Status.ToString())
        };

        for (var index = 0; index < rows.Length; index++)
        {
            sheet.Cell(index + 1, 1).Value = rows[index].Label;
            sheet.Cell(index + 1, 2).Value = rows[index].Value;
        }

        sheet.Column(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
    }

    private static void AddHoursSheet(XLWorkbook workbook, DailyReport dailyReport)
    {
        var sheet = workbook.Worksheets.Add("Godziny pracy");
        sheet.Cell(1, 1).Value = "Osoba";
        sheet.Cell(1, 2).Value = "Godziny";

        var rows = dailyReport.WorkHours.OrderBy(entry => entry.Worker?.LastName).ToList();
        for (var index = 0; index < rows.Count; index++)
        {
            sheet.Cell(index + 2, 1).Value = FullName(rows[index].Worker);
            sheet.Cell(index + 2, 2).Value = rows[index].Hours;
        }

        StyleWorksheet(sheet, 1, 2);
    }

    private static void AddWorkSheet(XLWorkbook workbook, DailyReport dailyReport)
    {
        var sheet = workbook.Worksheets.Add("Wykonane workEntries");
        sheet.Cell(1, 1).Value = "Typ";
        sheet.Cell(1, 2).Value = "Opis";
        sheet.Cell(1, 3).Value = "Ilość";

        var rows = dailyReport.WorkEntries.ToList();
        for (var index = 0; index < rows.Count; index++)
        {
            sheet.Cell(index + 2, 1).Value = rows[index].WorkTypeId.ToString();
            sheet.Cell(index + 2, 2).Value = rows[index].Description ?? "-";
            sheet.Cell(index + 2, 3).Value = rows[index].Quantity;
        }

        StyleWorksheet(sheet, 1, 3);
    }

    private static void AddMaterialsSheet(XLWorkbook workbook, DailyReport dailyReport, IReadOnlyDictionary<Guid, string> materialNames)
    {
        var sheet = workbook.Worksheets.Add("Zużyte materiały");
        sheet.Cell(1, 1).Value = "Materiał";
        sheet.Cell(1, 2).Value = "Ilość";

        var rows = dailyReport.MaterialUsages.ToList();
        for (var index = 0; index < rows.Count; index++)
        {
            sheet.Cell(index + 2, 1).Value = materialNames.GetValueOrDefault(rows[index].MaterialId, rows[index].MaterialId.ToString());
            sheet.Cell(index + 2, 2).Value = rows[index].Quantity;
        }

        StyleWorksheet(sheet, 1, 2);
    }

    private static byte[] SaveWorkbook(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void StyleWorksheet(IXLWorksheet sheet, int headerRow, int lastColumn)
    {
        var header = sheet.Range(headerRow, 1, headerRow, lastColumn);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
        sheet.Columns().AdjustToContents();
    }

    private static void AddPdfTable(
        ColumnDescriptor column,
        string title,
        string[] headers,
        IEnumerable<string[]> rows)
    {
        column.Item().PaddingTop(12).Text(title).FontSize(14).Bold();
        column.Item().PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in headers)
                {
                    columns.RelativeColumn();
                }
            });

            table.Header(header =>
            {
                foreach (var item in headers)
                {
                    header.Cell().Element(HeaderCellStyle).Text(item);
                }
            });

            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    table.Cell().Element(CellStyle).Text(cell);
                }
            }
        });
    }

    private static string FullName(Worker? person)
    {
        return person is null ? "-" : $"{person.FirstName} {person.LastName}";
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container.Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten3).Padding(5);
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5);
    }
}
