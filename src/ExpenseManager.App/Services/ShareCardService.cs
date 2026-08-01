using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using ExpenseManager.Core.Models;

namespace ExpenseManager.App.Services;

public enum ShareCardSize
{
    Square,   // 1080x1080 — Instagram feed / general
    Story,    // 1080x1920 — Instagram/Snapchat/WhatsApp status
    Landscape // 1600x900  — Twitter/X, LinkedIn, wide previews
}

/// <summary>Renders a branded PNG "spending summary card" for sharing to social apps. Uses plain
/// GDI+ (System.Drawing) rather than rendering a live XAML visual tree — WinUI's
/// RenderTargetBitmap only works on elements that are actually laid out inside a visible page, and
/// that's a lot of fragile plumbing for what's fundamentally a fixed-size image export. Windows-only
/// usage of System.Drawing.Common is fully supported; this app never runs anywhere else.</summary>
public class ShareCardService(CurrencyService currencyService)
{
    public string GenerateCard(DashboardSummary summary, DateTime from, DateTime to, ShareCardSize size, string outputPath)
    {
        var (width, height) = size switch
        {
            ShareCardSize.Square => (1080, 1080),
            ShareCardSize.Story => (1080, 1920),
            ShareCardSize.Landscape => (1600, 900),
            _ => (1080, 1080)
        };

        // Scaled off the shorter dimension so the tighter-vertical Landscape format doesn't
        // overflow — Square and Story both have a 1080 shorter side, so they render at full scale.
        var s = Math.Min(width, height) / 1080f;
        var footerReserve = 90f * s;

        // Content height is data-dependent (0-3 category rows) and the three canvas sizes have
        // very different aspect ratios, so pinning content to the top leaves an ugly dead zone
        // at the bottom of the tall Story format. Measure the natural content height on a
        // throwaway surface first (GDI+ happily draws past a bitmap's bounds — the pixels are
        // just clipped, coordinates and measured metrics are unaffected), then center it for
        // real. Squares/Landscapes have little slack, so this only visibly shifts Story.
        float naturalContentBottom;
        using (var probeBitmap = new Bitmap(1, 1))
        using (var probeG = Graphics.FromImage(probeBitmap))
            naturalContentBottom = DrawContent(probeG, summary, from, to, width, s, topOffset: 0f);

        var topOffset = Math.Max(0f, (height - footerReserve - naturalContentBottom) / 2f);

        using var bitmap = new Bitmap(width, height);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        DrawBackground(g, width, height);
        DrawContent(g, summary, from, to, width, s, topOffset);
        DrawFooter(g, width, height, s);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        bitmap.Save(outputPath, ImageFormat.Png);
        return outputPath;
    }

    private float DrawContent(Graphics g, DashboardSummary summary, DateTime from, DateTime to, int width, float s, float topOffset)
    {
        var padding = 70f * s;
        var contentWidth = width - 2 * padding;
        var y = padding + topOffset;

        y = DrawHeader(g, padding, y, s);
        y += 24f * s;

        using (var periodFont = new Font("Segoe UI", 24f * s))
        {
            var periodText = from.Date == to.Date ? from.ToString("MMM d, yyyy") : $"{from:MMM d} – {to:MMM d, yyyy}";
            using var periodBrush = new SolidBrush(Color.FromArgb(210, 255, 255, 255));
            g.DrawString(periodText, periodFont, periodBrush, padding, y);
            y += periodFont.GetHeight(g) + 48f * s;
        }

        y = DrawBalance(g, summary, padding, y, s);
        y += 56f * s;

        y = DrawIncomeExpenseRow(g, summary, padding, contentWidth, y, s);
        y += 44f * s;

        return DrawTopCategories(g, summary, padding, contentWidth, y, s);
    }

    private static void DrawBackground(Graphics g, int width, int height)
    {
        using var brush = new LinearGradientBrush(
            new Point(0, 0), new Point(width, height),
            Color.FromArgb(99, 102, 241),   // brand indigo
            Color.FromArgb(67, 56, 202));   // deeper indigo
        g.FillRectangle(brush, 0, 0, width, height);
    }

    private static float DrawHeader(Graphics g, float x, float y, float s)
    {
        var chipSize = 56f * s;
        using (var chipBrush = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
        using (var path = RoundedRect(x, y, chipSize, chipSize, 14f * s))
            g.FillPath(chipBrush, path);

        using (var iconFont = new Font("Segoe UI", 26f * s, FontStyle.Bold))
        using (var iconBrush = new SolidBrush(Color.White))
        using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            g.DrawString("$", iconFont, iconBrush, new RectangleF(x, y, chipSize, chipSize), format);

        using var titleFont = new Font("Segoe UI Semibold", 32f * s, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Color.White);
        var titleY = y + (chipSize - titleFont.GetHeight(g)) / 2;
        g.DrawString("Expense Manager Pro", titleFont, titleBrush, x + chipSize + 18f * s, titleY);

        return y + chipSize;
    }

    private float DrawBalance(Graphics g, DashboardSummary summary, float x, float y, float s)
    {
        var saved = summary.Balance >= 0;
        var label = saved ? "NET SAVED" : "NET SPENT";

        using (var labelFont = new Font("Segoe UI Semibold", 22f * s, FontStyle.Bold))
        using (var labelBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
        {
            g.DrawString(label, labelFont, labelBrush, x, y);
            y += labelFont.GetHeight(g) + 4f * s;
        }

        using var amountFont = new Font("Segoe UI", 92f * s, FontStyle.Bold);
        using var amountBrush = new SolidBrush(Color.White);
        g.DrawString(currencyService.FormatPlain(Math.Abs(summary.Balance)), amountFont, amountBrush, x, y);
        return y + amountFont.GetHeight(g);
    }

    private float DrawIncomeExpenseRow(Graphics g, DashboardSummary summary, float x, float width, float y, float s)
    {
        var colWidth = width / 2;
        DrawStat(g, "Income", currencyService.FormatPlain(summary.TotalIncome), x, y, s);
        DrawStat(g, "Expenses", currencyService.FormatPlain(summary.TotalExpense), x + colWidth, y, s);

        using var labelFont = new Font("Segoe UI Semibold", 20f * s, FontStyle.Bold);
        using var amountFont = new Font("Segoe UI", 38f * s, FontStyle.Bold);
        return y + labelFont.GetHeight(g) + 6f * s + amountFont.GetHeight(g);
    }

    private static void DrawStat(Graphics g, string label, string amount, float x, float y, float s)
    {
        using var labelFont = new Font("Segoe UI Semibold", 20f * s, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        g.DrawString(label.ToUpperInvariant(), labelFont, labelBrush, x, y);

        using var amountFont = new Font("Segoe UI", 38f * s, FontStyle.Bold);
        using var amountBrush = new SolidBrush(Color.White);
        g.DrawString(amount, amountFont, amountBrush, x, y + labelFont.GetHeight(g) + 6f * s);
    }

    private static float DrawTopCategories(Graphics g, DashboardSummary summary, float x, float width, float y, float s)
    {
        // Capped at 3 (not 4) so the list never collides with the footer — the card is a fixed
        // canvas size, there's no scrolling to fall back on if content runs long.
        var categories = summary.ExpenseByCategory.Take(3).ToList();
        if (categories.Count == 0) return y;

        using (var headerFont = new Font("Segoe UI Semibold", 24f * s, FontStyle.Bold))
        using (var headerBrush = new SolidBrush(Color.White))
        {
            g.DrawString("TOP CATEGORIES", headerFont, headerBrush, x, y);
            y += headerFont.GetHeight(g) + 20f * s;
        }

        var maxTotal = categories.Max(c => c.Total);
        using var nameFont = new Font("Segoe UI", 24f * s);
        using var amountFont = new Font("Segoe UI Semibold", 24f * s, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        using var trackBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
        // Category colors are user-chosen and can land on the same brand-indigo hue as the
        // card's own background gradient — without an outline that color's dot/bar would be
        // nearly invisible. A thin translucent-white ring keeps every color legible regardless.
        using var outlinePen = new Pen(Color.FromArgb(140, 255, 255, 255), 1.5f * s);

        foreach (var category in categories)
        {
            var dotSize = 16f * s;
            var dotY = y + (nameFont.GetHeight(g) - dotSize) / 2;
            using (var dotBrush = new SolidBrush(HexToColor(category.Color)))
                g.FillEllipse(dotBrush, x, dotY, dotSize, dotSize);
            g.DrawEllipse(outlinePen, x, dotY, dotSize, dotSize);

            g.DrawString(category.CategoryName, nameFont, textBrush, x + dotSize + 14f * s, y);

            using var format = new StringFormat { Alignment = StringAlignment.Far };
            var amountText = category.Total.ToString("N0");
            g.DrawString(amountText, amountFont, textBrush, new RectangleF(x, y, width, amountFont.GetHeight(g) + 4), format);

            y += nameFont.GetHeight(g) + 8f * s;

            var barHeight = 6f * s;
            var barWidth = width * (float)(category.Total / maxTotal);
            using (var barPath = RoundedRect(x, y, width, barHeight, barHeight / 2))
                g.FillPath(trackBrush, barPath);
            if (barWidth > barHeight)
            {
                using var barPath = RoundedRect(x, y, barWidth, barHeight, barHeight / 2);
                using var barBrush = new SolidBrush(HexToColor(category.Color));
                g.FillPath(barBrush, barPath);
                g.DrawPath(outlinePen, barPath);
            }

            y += barHeight + 22f * s;
        }

        return y;
    }

    private static void DrawFooter(Graphics g, int width, int height, float s)
    {
        using var font = new Font("Segoe UI", 18f * s);
        using var brush = new SolidBrush(Color.FromArgb(160, 255, 255, 255));
        using var format = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString("Generated with Expense Manager Pro", font, brush,
            new RectangleF(0, height - 60f * s, width, 40f * s), format);
    }

    private static GraphicsPath RoundedRect(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        var r = Convert.ToByte(hex.Substring(0, 2), 16);
        var g = Convert.ToByte(hex.Substring(2, 2), 16);
        var b = Convert.ToByte(hex.Substring(4, 2), 16);
        return Color.FromArgb(r, g, b);
    }
}
