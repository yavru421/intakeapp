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

        // Fonts
        var titleFont = new XFont("Roboto", 20, XFontStyle.Bold);
        var sectionFont = new XFont("Roboto", 14, XFontStyle.Bold);
        var subSectionFont = new XFont("Roboto", 11, XFontStyle.Bold);
        var normalFont = new XFont("Roboto", 10, XFontStyle.Regular);
        var italicFont = new XFont("Roboto", 9, XFontStyle.Italic);
        var codeFont = new XFont("Courier New", 9, XFontStyle.Regular);

        // Helper to extract answers
        string GetAnswer(string qId) => request.Answers.FirstOrDefault(a => a.QuestionId == qId)?.AnswerText ?? "N/A";

        var domain = GetAnswer("start");
        var actor = request.Answers.FirstOrDefault(a => a.QuestionId == "q_field_actor" || a.QuestionId == "q_office_actor" || a.QuestionId == "q_customer_actor")?.AnswerText ?? "N/A";
        var volume = GetAnswer("q_volume");
        var input = GetAnswer("q_input");
        var offline = GetAnswer("q_offline");
        var location = GetAnswer("q_location");
        var auth = GetAnswer("q_auth");
        var output = GetAnswer("q_output");
        var money = GetAnswer("q_money");
        var notifications = GetAnswer("q_notifications");
        var integrations = GetAnswer("q_integrations");
        var process = GetAnswer("q_process");
        var timeline = GetAnswer("q_timeline");

        // ---------------- PAGE 1: EXECUTIVE SUMMARY ----------------
        var page1 = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page1);

        // Banner Header
        var headerRect = new XRect(0, 0, page1.Width, 90);
        gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(27, 38, 59)), headerRect);
        gfx.DrawString("DONDLINGER DIGITAL", subSectionFont, XBrushes.LightGray, new XRect(40, 20, page1.Width, 20), XStringFormats.TopLeft);
        gfx.DrawString("SOFTWARE ARCHITECTURAL BLUEPRINT", titleFont, XBrushes.White, new XRect(40, 40, page1.Width, 40), XStringFormats.TopLeft);

        double y = 120;
        gfx.DrawString("Executive Summary", sectionFont, XBrushes.MidnightBlue, 40, y);
        y += 20;

        string summaryText = $"Based on your solution intake assessment, we have compiled an automated blueprint mapping your requirement profile to a modern, serverless Cloudflare Workers & D1 SQL architecture. This blueprint outlines your software service flow, database schema, and recommended technology stack.";
        y = DrawWrappedText(gfx, summaryText, normalFont, 40, y, page1.Width - 80);
        y += 25;

        // Card details
        gfx.DrawString("Solution Profile", sectionFont, XBrushes.MidnightBlue, 40, y);
        y += 20;

        y = DrawSummaryRow(gfx, "Primary Domain", domain, normalFont, italicFont, y);
        y = DrawSummaryRow(gfx, "Primary Actor", actor, normalFont, italicFont, y);
        y = DrawSummaryRow(gfx, "Daily Active Users", volume, normalFont, italicFont, y);
        y = DrawSummaryRow(gfx, "Data Entry Type", input, normalFont, italicFont, y);
        y = DrawSummaryRow(gfx, "Offline Requirement", offline, normalFont, italicFont, y);
        y = DrawSummaryRow(gfx, "GPS/Geofencing", location, normalFont, italicFont, y);
        y = DrawSummaryRow(gfx, "Authentication Setup", auth, normalFont, italicFont, y);
        y = DrawSummaryRow(gfx, "Integration Profile", integrations, normalFont, italicFont, y);
        y = DrawSummaryRow(gfx, "Development Timeline", timeline, normalFont, italicFont, y);

        DrawFooter(gfx, page1, request, italicFont);

        // ---------------- PAGE 2: ARCHITECTURE FLOWCHART ----------------
        var page2 = document.AddPage();
        var gfx2 = XGraphics.FromPdfPage(page2);

        // Header
        gfx2.DrawRectangle(new XSolidBrush(XColor.FromArgb(27, 38, 59)), new XRect(0, 0, page2.Width, 50));
        gfx2.DrawString("System Architecture Flow Chart", sectionFont, XBrushes.White, new XRect(40, 15, page2.Width, 30), XStringFormats.TopLeft);

        double flowY = 80;
        gfx2.DrawString("Below is the flow mapping deduced for this project profile:", subSectionFont, XBrushes.DarkSlateGray, 40, flowY);

        // Center column positions
        double midX = page2.Width / 2; // ~306
        double boxW = 180;
        double boxH = 45;
        double leftAlignX = midX - (boxW / 2);

        // Node 1: Client Front-End
        string clientTitle = "Client Interface";
        string clientSub = "Web App (PWA)";
        if (offline.Contains("continue working"))
        {
            clientTitle = "Offline PWA Client";
            clientSub = "IndexedDB Sync Engine";
        }
        DrawBox(gfx2, clientTitle, clientSub, subSectionFont, italicFont, leftAlignX, 120, boxW, boxH);

        // Arrow 1 -> Worker API
        DrawArrow(gfx2, midX, 165, midX, 210);

        // Node 2: Worker API
        string workerTitle = "Cloudflare Worker";
        string workerSub = "ES Module API Router";
        DrawBox(gfx2, workerTitle, workerSub, subSectionFont, italicFont, leftAlignX, 210, boxW, boxH);

        // Arrow 2 -> D1 SQL
        DrawArrow(gfx2, midX, 255, midX, 300);

        // Node 3: SQL Database
        string dbTitle = "Cloudflare D1 SQL";
        string dbSub = "Persistent SQLite Engine";
        DrawBox(gfx2, dbTitle, dbSub, subSectionFont, italicFont, leftAlignX, 300, boxW, boxH);

        // Side Integrations depending on answers
        double sideX = leftAlignX + boxW + 50;

        // If Media Uploads: Cloudflare R2 Box
        if (input.Contains("photos") || input.Contains("uploading"))
        {
            DrawBox(gfx2, "Cloudflare R2 Bucket", "Object Asset Store", subSectionFont, italicFont, sideX, 210, 150, boxH);
            DrawArrow(gfx2, leftAlignX + boxW, 232.5, sideX, 232.5);
        }

        // If Financial/Stripe Box
        if (money.Contains("Stripe") || money.Contains("invoice"))
        {
            DrawBox(gfx2, "Stripe API Gateway", "Financial Services", subSectionFont, italicFont, sideX, 120, 150, boxH);
            // Draw a diagonal or straight connector from Worker
            gfx2.DrawLine(new XPen(XColors.SlateGray, 1), leftAlignX + boxW, 220, sideX, 142.5);
            gfx2.DrawLine(new XPen(XColors.SlateGray, 1), sideX, 142.5, sideX - 5, 142.5 + 5);
        }

        // If SMS/Notifications
        if (notifications.Contains("SMS") || notifications.Contains("Push"))
        {
            string gate = notifications.Contains("SMS") ? "Twilio SMS Gateway" : "Firebase Cloud Message";
            DrawBox(gfx2, gate, "Real-time Notification", subSectionFont, italicFont, 20, 210, 140, boxH);
            DrawArrow(gfx2, leftAlignX, 232.5, 160, 232.5); // points left
        }

        DrawFooter(gfx2, page2, request, italicFont);

        // ---------------- PAGE 3: TECH STACK & SCHEMA ----------------
        var page3 = document.AddPage();
        var gfx3 = XGraphics.FromPdfPage(page3);

        gfx3.DrawRectangle(new XSolidBrush(XColor.FromArgb(27, 38, 59)), new XRect(0, 0, page3.Width, 50));
        gfx3.DrawString("Infrastructure & Database Recommendation", sectionFont, XBrushes.White, new XRect(40, 15, page3.Width, 30), XStringFormats.TopLeft);

        double secY = 70;
        gfx3.DrawString("Recommended Tech Stack", subSectionFont, XBrushes.MidnightBlue, 40, secY);
        secY += 20;

        gfx3.DrawString("- Hosting / CDN: Cloudflare Pages & Workers Assets", normalFont, XBrushes.Black, 50, secY); secY += 15;
        gfx3.DrawString("- API Router: Cloudflare Workers (JavaScript/TypeScript ES Modules)", normalFont, XBrushes.Black, 50, secY); secY += 15;
        gfx3.DrawString("- Database Store: Cloudflare D1 SQL (SQLite compatible)", normalFont, XBrushes.Black, 50, secY); secY += 15;
        if (offline.Contains("continue working"))
        {
            gfx3.DrawString("- Client Sync Engine: Service Worker + local IndexedDB cache", normalFont, XBrushes.Black, 50, secY); secY += 15;
        }
        else
        {
            gfx3.DrawString("- Front-End Engine: Single Page Application (Blazor / React)", normalFont, XBrushes.Black, 50, secY); secY += 15;
        }

        secY += 15;
        gfx3.DrawString("Required Database Tables (SQL DDL recommendation)", subSectionFont, XBrushes.MidnightBlue, 40, secY);
        secY += 20;

        secY = DrawCodeLine(gfx3, "CREATE TABLE users (id TEXT PRIMARY KEY, email TEXT, role TEXT);", codeFont, 50, secY);
        if (offline.Contains("continue working"))
        {
            secY = DrawCodeLine(gfx3, "CREATE TABLE sync_queue (id TEXT PRIMARY KEY, payload TEXT, status TEXT);", codeFont, 50, secY);
        }
        if (location.Contains("GPS") || location.Contains("address"))
        {
            secY = DrawCodeLine(gfx3, "CREATE TABLE locations (id TEXT PRIMARY KEY, lat REAL, lng REAL, user_id TEXT);", codeFont, 50, secY);
        }
        if (input.Contains("photos") || input.Contains("uploading"))
        {
            secY = DrawCodeLine(gfx3, "CREATE TABLE media (id TEXT PRIMARY KEY, url TEXT, uploaded_at TEXT);", codeFont, 50, secY);
        }
        if (money.Contains("Stripe") || money.Contains("invoice"))
        {
            secY = DrawCodeLine(gfx3, "CREATE TABLE transactions (id TEXT PRIMARY KEY, amount INT, status TEXT);", codeFont, 50, secY);
        }
        secY = DrawCodeLine(gfx3, "CREATE TABLE logs (id TEXT PRIMARY KEY, message TEXT, timestamp TEXT);", codeFont, 50, secY);

        DrawFooter(gfx3, page3, request, italicFont);

        using (var stream = new MemoryStream())
        {
            document.Save(stream, false);
            return stream.ToArray();
        }
    }

    private void DrawBox(XGraphics gfx, string title, string subtitle, XFont titleFont, XFont subFont, double x, double y, double width, double height)
    {
        var rect = new XRect(x, y, width, height);
        // Draw elegant rounded box
        gfx.DrawRoundedRectangle(new XPen(XColor.FromArgb(74, 85, 104), 1.5), new XSolidBrush(XColor.FromArgb(247, 250, 252)), rect, new XSize(6, 6));

        // Draw title
        var titleRect = new XRect(x, y + 8, width, height / 2);
        gfx.DrawString(title, titleFont, XBrushes.DarkSlateGray, titleRect, XStringFormats.TopCenter);

        // Draw subtitle
        var subRect = new XRect(x, y + 25, width, height / 2);
        gfx.DrawString(subtitle, subFont, XBrushes.SlateGray, subRect, XStringFormats.TopCenter);
    }

    private void DrawArrow(XGraphics gfx, double x1, double y1, double x2, double y2)
    {
        var pen = new XPen(XColor.FromArgb(113, 128, 150), 1.5);
        gfx.DrawLine(pen, x1, y1, x2, y2);

        // Draw arrow tip
        if (x1 == x2) // Downward arrow
        {
            gfx.DrawLine(pen, x2, y2, x2 - 4, y2 - 6);
            gfx.DrawLine(pen, x2, y2, x2 + 4, y2 - 6);
        }
        else if (x1 < x2) // Rightward arrow
        {
            gfx.DrawLine(pen, x2, y2, x2 - 6, y2 - 4);
            gfx.DrawLine(pen, x2, y2, x2 - 6, y2 + 4);
        }
        else if (x1 > x2) // Leftward arrow
        {
            gfx.DrawLine(pen, x2, y2, x2 + 6, y2 - 4);
            gfx.DrawLine(pen, x2, y2, x2 + 6, y2 + 4);
        }
    }

    private double DrawSummaryRow(XGraphics gfx, string label, string value, XFont font, XFont valueFont, double y)
    {
        gfx.DrawString(label + ":", font, XBrushes.Black, 50, y);
        gfx.DrawString(value, valueFont, XBrushes.DarkSlateGray, 220, y);
        gfx.DrawLine(new XPen(XColors.Gainsboro, 0.5), 50, y + 6, 550, y + 6);
        return y + 24;
    }

    private double DrawWrappedText(XGraphics gfx, string text, XFont font, double x, double y, double width)
    {
        var formatter = new XTextFormatter(gfx);
        var rect = new XRect(x, y, width, 80);
        formatter.DrawString(text, font, XBrushes.Black, rect);
        return y + 45;
    }

    private double DrawCodeLine(XGraphics gfx, string line, XFont font, double x, double y)
    {
        var rect = new XRect(x, y, 500, 20);
        gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(245, 245, 245)), rect);
        gfx.DrawString(line, font, XBrushes.MidnightBlue, x + 10, y + 13);
        return y + 24;
    }

    private void DrawFooter(XGraphics gfx, PdfPage page, IntakeRequest request, XFont font)
    {
        double yPos = page.Height - 35;
        gfx.DrawLine(new XPen(XColors.LightGray, 1), 40, yPos - 5, page.Width - 40, yPos - 5);
        gfx.DrawString($"ID: {request.Id} | Compiled at {request.Timestamp.ToString("f")}", font, XBrushes.Gray, 40, yPos + 8);
        gfx.DrawString($"Device: {request.DeviceMetadata.Split(' ').FirstOrDefault() ?? "PWA Client"}", font, XBrushes.Gray, page.Width - 140, yPos + 8);
    }
}
