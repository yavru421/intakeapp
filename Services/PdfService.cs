using System;
using System.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using IntakeApp.Models;

namespace IntakeApp.Services;

public class PdfService
{
    public byte[] GenerateProjectRequestSummary(IntakeRequest request)
    {
        var document = new PdfDocument();
        document.Info.Title = "Project Request Summary";
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        // Fonts
        var titleFont = new XFont("Helvetica", 22, XFontStyle.Bold);
        var headingFont = new XFont("Helvetica", 14, XFontStyle.Bold);
        var normalFont = new XFont("Helvetica", 12, XFontStyle.Regular);
        var smallFont = new XFont("Helvetica", 10, XFontStyle.Italic);

        int yPosition = 40;
        int leftMargin = 40;
        double maxWidth = page.Width - leftMargin * 2;

        // Draw Header Background
        var headerRect = new XRect(0, 0, page.Width, 80);
        gfx.DrawRectangle(XBrushes.SteelBlue, headerRect);

        // Title
        gfx.DrawString("Project Request Summary", titleFont, XBrushes.White, new XRect(0, 0, page.Width, 80), XStringFormats.Center);
        yPosition = 110;

        // Draw Fields
        yPosition = DrawField(gfx, "Project Goal", request.ProjectGoal, headingFont, normalFont, leftMargin, yPosition, maxWidth);
        yPosition = DrawField(gfx, "Business Impact", request.BusinessImpact, headingFont, normalFont, leftMargin, yPosition, maxWidth);
        yPosition = DrawField(gfx, "Service Options", request.ServiceOptions != null && request.ServiceOptions.Count > 0 ? string.Join(", ", request.ServiceOptions) : "None", headingFont, normalFont, leftMargin, yPosition, maxWidth);
        yPosition = DrawField(gfx, "Budget Alignment", request.BudgetAlignment, headingFont, normalFont, leftMargin, yPosition, maxWidth);
        
        // Footer (Timestamp and Device)
        gfx.DrawLine(new XPen(XColors.LightGray, 1), leftMargin, yPosition + 10, page.Width - leftMargin, yPosition + 10);
        yPosition += 30;
        gfx.DrawString($"Submitted: {request.Timestamp.ToString("f")}", smallFont, XBrushes.Gray, leftMargin, yPosition);
        yPosition += 15;
        gfx.DrawString($"Device Info: {request.DeviceMetadata}", smallFont, XBrushes.Gray, leftMargin, yPosition);

        using (var stream = new MemoryStream())
        {
            document.Save(stream, false);
            return stream.ToArray();
        }
    }

    private int DrawField(XGraphics gfx, string label, string value, XFont labelFont, XFont valueFont, int x, int y, double maxWidth)
    {
        // Draw Label
        gfx.DrawString(label, labelFont, XBrushes.Black, x, y);
        y += 20;

        // Draw Value with Wrapping
        var formatter = new XTextFormatter(gfx);
        // Estimate height needed
        int estimatedHeight = 60;
        var rect = new XRect(x, y, maxWidth, estimatedHeight);
        
        // Use a background for the value
        var bgRect = new XRect(x - 5, y - 5, maxWidth + 10, estimatedHeight + 10);
        gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(245, 245, 245)), bgRect);

        formatter.DrawString(value ?? "N/A", valueFont, XBrushes.DarkSlateGray, rect);
        
        return y + estimatedHeight + 25;
    }
}
