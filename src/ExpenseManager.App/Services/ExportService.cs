using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Models;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace ExpenseManager.App.Services;

public record TransactionExportRow(DateTime Date, string Type, string Category, string Wallet, decimal Amount, string? Note);

/// <summary>PdfSharp 6 has no automatic access to installed system fonts (unlike the live WinUI
/// UI, which goes through the OS text stack with full font fallback) — it requires an explicit
/// resolver. This one just reads Arial straight off disk, which every Windows install has.</summary>
internal class WindowsArialFontResolver : IFontResolver
{
    public byte[] GetFont(string faceName)
    {
        var path = faceName switch
        {
            "Arial#Bold" => @"C:\Windows\Fonts\arialbd.ttf",
            "Arial#Italic" => @"C:\Windows\Fonts\ariali.ttf",
            "Arial#BoldItalic" => @"C:\Windows\Fonts\arialbi.ttf",
            _ => @"C:\Windows\Fonts\arial.ttf"
        };
        return File.ReadAllBytes(path);
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var faceName = (isBold, isItalic) switch
        {
            (true, false) => "Arial#Bold",
            (false, true) => "Arial#Italic",
            (true, true) => "Arial#BoldItalic",
            _ => "Arial#Regular"
        };
        return new FontResolverInfo(faceName);
    }
}

public class ExportService(CurrencyService currencyService)
{
    private static bool _fontResolverRegistered;

    private static void EnsureFontResolverRegistered()
    {
        if (_fontResolverRegistered) return;
        GlobalFontSettings.FontResolver = new WindowsArialFontResolver();
        _fontResolverRegistered = true;
    }

    private static List<TransactionExportRow> ToRows(IEnumerable<Transaction> transactions) =>
        transactions
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionExportRow(
                t.Date,
                t.Type.ToString(),
                t.Category?.Name ?? "Uncategorized",
                t.Wallet?.Name ?? "Unknown",
                t.Amount,
                t.Note))
            .ToList();

    public async Task ExportCsvAsync(List<Transaction> transactions, string filePath, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Type,Category,Wallet,Amount,Note");
        foreach (var row in ToRows(transactions))
        {
            sb.AppendLine(string.Join(",",
                row.Date.ToString("yyyy-MM-dd"),
                row.Type,
                CsvEscape(row.Category),
                CsvEscape(row.Wallet),
                row.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CsvEscape(row.Note ?? "")));
        }
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8, ct);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    public async Task ExportJsonAsync(List<Transaction> transactions, string filePath, CancellationToken ct = default)
    {
        var rows = ToRows(transactions);
        var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, ct);
    }

    public Task ExportExcelAsync(List<Transaction> transactions, string filePath, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Transactions");

            string[] headers = ["Date", "Type", "Category", "Wallet", "Amount", "Note"];
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xEE, 0xF0, 0xFE);
            }

            var rows = ToRows(transactions);
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var excelRow = i + 2;
                sheet.Cell(excelRow, 1).Value = r.Date;
                sheet.Cell(excelRow, 1).Style.DateFormat.Format = "yyyy-mm-dd";
                sheet.Cell(excelRow, 2).Value = r.Type;
                sheet.Cell(excelRow, 3).Value = r.Category;
                sheet.Cell(excelRow, 4).Value = r.Wallet;
                sheet.Cell(excelRow, 5).Value = r.Amount;
                sheet.Cell(excelRow, 6).Value = r.Note ?? "";
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }, ct);
    }

    public Task ExportPdfReportAsync(
        DashboardSummary summary,
        List<Transaction> transactions,
        DateTime from,
        DateTime to,
        string filePath,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            EnsureFontResolverRegistered();

            var document = new PdfDocument();
            var page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);

            var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
            var headerFont = new XFont("Arial", 12, XFontStyleEx.Bold);
            var bodyFont = new XFont("Arial", 10, XFontStyleEx.Regular);
            var mutedFont = new XFont("Arial", 9, XFontStyleEx.Regular);

            const double margin = 40;
            const double lineHeight = 18;
            double y = margin;
            double pageWidth = page.Width.Point - 2 * margin;

            void NewPageIfNeeded(double needed)
            {
                if (y + needed <= page.Height.Point - margin) return;
                page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                y = margin;
            }

            gfx.DrawString("Expense Manager Pro — Report", titleFont, XBrushes.Black, new XPoint(margin, y));
            y += lineHeight + 4;
            gfx.DrawString($"{from:MMM d, yyyy} – {to:MMM d, yyyy}", bodyFont, XBrushes.Gray, new XPoint(margin, y));
            y += lineHeight * 1.5;

            gfx.DrawString("Summary", headerFont, XBrushes.Black, new XPoint(margin, y));
            y += lineHeight;
            gfx.DrawString($"Income: {currencyService.FormatPlain(summary.TotalIncome)}", bodyFont, new XSolidBrush(XColor.FromArgb(0x16, 0xA3, 0x4A)), new XPoint(margin, y));
            y += lineHeight;
            gfx.DrawString($"Expenses: {currencyService.FormatPlain(summary.TotalExpense)}", bodyFont, new XSolidBrush(XColor.FromArgb(0xDC, 0x26, 0x26)), new XPoint(margin, y));
            y += lineHeight;
            gfx.DrawString($"Balance: {currencyService.FormatPlain(summary.Balance)}", bodyFont, XBrushes.Black, new XPoint(margin, y));
            y += lineHeight * 1.5;

            if (summary.ExpenseByCategory.Count > 0)
            {
                gfx.DrawString("Spending by category", headerFont, XBrushes.Black, new XPoint(margin, y));
                y += lineHeight;
                foreach (var category in summary.ExpenseByCategory)
                {
                    NewPageIfNeeded(lineHeight);
                    gfx.DrawString(category.CategoryName, bodyFont, XBrushes.Black, new XPoint(margin, y));
                    gfx.DrawString(currencyService.FormatPlain(category.Total), bodyFont, XBrushes.Black,
                        new XRect(margin, y - 12, pageWidth, lineHeight), XStringFormats.TopRight);
                    y += lineHeight;
                }
                y += lineHeight * 0.5;
            }

            NewPageIfNeeded(lineHeight * 2);
            gfx.DrawString("Transactions", headerFont, XBrushes.Black, new XPoint(margin, y));
            y += lineHeight;

            double col1 = margin, col2 = margin + 70, col3 = margin + 220, col4 = margin + 340;
            gfx.DrawString("Date", mutedFont, XBrushes.Gray, new XPoint(col1, y));
            gfx.DrawString("Category", mutedFont, XBrushes.Gray, new XPoint(col2, y));
            gfx.DrawString("Note", mutedFont, XBrushes.Gray, new XPoint(col3, y));
            gfx.DrawString("Amount", mutedFont, XBrushes.Gray, new XPoint(col4, y));
            y += lineHeight;

            foreach (var row in ToRows(transactions))
            {
                NewPageIfNeeded(lineHeight);
                gfx.DrawString(row.Date.ToString("MMM d, yyyy"), bodyFont, XBrushes.Black, new XPoint(col1, y));
                gfx.DrawString(Truncate(row.Category, 22), bodyFont, XBrushes.Black, new XPoint(col2, y));
                gfx.DrawString(Truncate(row.Note ?? "", 18), bodyFont, XBrushes.Black, new XPoint(col3, y));
                var amountBrush = row.Type == "Income" ? new XSolidBrush(XColor.FromArgb(0x16, 0xA3, 0x4A)) : new XSolidBrush(XColor.FromArgb(0xDC, 0x26, 0x26));
                gfx.DrawString(currencyService.FormatPlain(row.Amount), bodyFont, amountBrush, new XPoint(col4, y));
                y += lineHeight;
            }

            document.Save(filePath);
        }, ct);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}
