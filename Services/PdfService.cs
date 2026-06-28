using System;
using System.IO;
using System.Linq;
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
        document.Info.Title = "Architectural Blueprint";
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        // Fonts
        var titleFont = new XFont("Roboto", 22, XFontStyle.Bold);
        var headingFont = new XFont("Roboto", 14, XFontStyle.Bold);
        var normalFont = new XFont("Roboto", 12, XFontStyle.Regular);
        var smallFont = new XFont("Roboto", 10, XFontStyle.Italic);

        int yPosition = 40;
        int leftMargin = 40;
        double maxWidth = page.Width - leftMargin * 2;

        // Draw Header Background
        var headerRect = new XRect(0, 0, page.Width, 80);
        gfx.DrawRectangle(XBrushes.SteelBlue, headerRect);

        // Title
        gfx.DrawString("Architectural Blueprint", titleFont, XBrushes.White, new XRect(0, 0, page.Width, 80), XStringFormats.Center);
        yPosition = 110;

        // Draw Fields dynamically based on answers
        if (request.Answers != null && request.Answers.Any())
        {
            foreach (var answer in request.Answers)
            {
                // Check if we need a new page
                if (yPosition > page.Height - 100)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPosition = 40;
                }
                
                yPosition = DrawField(gfx, answer.QuestionText, answer.AnswerText, headingFont, normalFont, leftMargin, yPosition, maxWidth);
            }
        }
        else
        {
            yPosition = DrawField(gfx, "Status", "No data provided", headingFont, normalFont, leftMargin, yPosition, maxWidth);
        }
        
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
        int estimatedHeight = 40;
        var rect = new XRect(x, y, maxWidth, estimatedHeight);
        
        // Use a background for the value
        var bgRect = new XRect(x - 5, y - 5, maxWidth + 10, estimatedHeight + 10);
        gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(245, 245, 245)), bgRect);

        formatter.DrawString(value ?? "N/A", valueFont, XBrushes.DarkSlateGray, rect);
        
        return y + estimatedHeight + 20;
    }
}
