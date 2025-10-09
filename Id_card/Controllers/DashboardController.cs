using Id_card.Models;
using Id_card.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace Id_card.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ICardDbContext _context;
        private readonly EmailService _emailService;

        public DashboardController(ICardDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Users");
            }

            // Get dashboard statistics
            var totalEmployees = await _context.Employees.CountAsync();
            var idCardsGenerated = await _context.Employees.CountAsync(e => e.PrintedStatus);
            var activeEmployees = await _context.Employees.CountAsync(e => e.IsActive);
            var inactiveEmployees = await _context.Employees.CountAsync(e => !e.IsActive);
            var emailsSent = await _context.Employees.CountAsync(e => e.SentOnMailStatus);

            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.IdCardsGenerated = idCardsGenerated;
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.InactiveEmployees = inactiveEmployees;
            ViewBag.EmailsSent = emailsSent;
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "User";

            return View();
        }

        // Employee Lists for each indicator
        public async Task<IActionResult> EmployeeList(string type)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            var query = _context.Employees.Include(e => e.Address).AsQueryable();

            switch (type.ToLower())
            {
                case "total":
                    break;
                case "generated":
                    query = query.Where(e => e.PrintedStatus);
                    break;
                case "active":
                    query = query.Where(e => e.IsActive);
                    break;
                case "inactive":
                    query = query.Where(e => !e.IsActive);
                    break;
                case "emailsent":
                    query = query.Where(e => e.SentOnMailStatus);
                    break;
            }

            var employees = await query.OrderByDescending(e => e.EmployeeId).ToListAsync();
            ViewBag.ListType = type;
            return View(employees);
        }

        // Generate ID Card as PDF
        public async Task<IActionResult> GenerateIdCardPdf(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            var employee = await _context.Employees
                .Include(e => e.Address)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("Index");
            }

            // Generate QR Code
            var qrData = $"Employee ID: {employee.EmployeeId}\nName: {employee.Name}\nDepartment: {employee.Department}\nDesignation: {employee.Designation}\nEmail: {employee.Email}";
            var qrCodeImage = GenerateQRCode(qrData);

            // Create PDF
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            // Add ID Card content to PDF
            await AddIdCardToPdf(document, employee, qrCodeImage);

            document.Close();

            var pdfBytes = memoryStream.ToArray();
            return File(pdfBytes, "application/pdf", $"IDCard_{employee.EmployeeId}_{employee.Name.Replace(" ", "_")}.pdf");
        }

        // Send ID Card via Email (PDF attachment only)
        public async Task<IActionResult> SendIdCardEmail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            var employee = await _context.Employees
                .Include(e => e.Address)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null || string.IsNullOrEmpty(employee.Email))
            {
                TempData["Error"] = "Employee not found or email not available.";
                return RedirectToAction("Index");
            }

            try
            {
                // Generate QR Code
                var qrData = $"Employee ID: {employee.EmployeeId}\nName: {employee.Name}\nDepartment: {employee.Department}\nDesignation: {employee.Designation}\nEmail: {employee.Email}";
                var qrCodeImage = GenerateQRCode(qrData);

                // Build PDF in memory
                byte[] pdfBytes;
                using (var memoryStream = new MemoryStream())
                {
                    using var writer = new PdfWriter(memoryStream);
                    using var pdf = new PdfDocument(writer);
                    using var document = new Document(pdf);
                    await AddIdCardToPdf(document, employee, qrCodeImage);
                    document.Close();
                    pdfBytes = memoryStream.ToArray();
                }

                // Save PDF to temp and attach
                var tmpDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tmp");
                if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);
                var pdfPath = Path.Combine(tmpDir, $"IDCard_{employee.EmployeeId}_{employee.Name.Replace(" ", "_")}.pdf");
                await System.IO.File.WriteAllBytesAsync(pdfPath, pdfBytes);

                await _emailService.SendEmailAsync(employee.Email, "Your ID Card", "Please find attached your ID card.", pdfPath);

                employee.SentOnMailStatus = true;
                _context.Update(employee);
                await _context.SaveChangesAsync();

                TempData["Message"] = "ID Card sent successfully to employee's email.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to send email: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Get WhatsApp share link
        public IActionResult GetWhatsAppLink(int id)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeId == id);
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found" });
            }

            var message = $"ID Card for {employee.Name} (Employee ID: {employee.EmployeeId})";
            var whatsappUrl = $"https://wa.me/?text={Uri.EscapeDataString(message)}";
            
            return Json(new { success = true, url = whatsappUrl });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Users");
        }

        private async Task AddIdCardToPdf(Document document, Employees employee, string qrCodeImage)
        {
            // Create a table for the ID card layout
            var table = new Table(2).UseAllAvailableWidth();
            
            // Left column - Employee details
            var leftCell = new Cell();
            leftCell.Add(new Paragraph("iCard System")
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER));
            leftCell.Add(new Paragraph("Employee Identity Card")
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));
            leftCell.Add(new Paragraph($"ID: {employee.EmployeeId}").SetFontSize(10));
            leftCell.Add(new Paragraph($"Name: {employee.Name}").SetFontSize(10));
            leftCell.Add(new Paragraph($"Dept: {employee.Department}").SetFontSize(10));
            leftCell.Add(new Paragraph($"Designation: {employee.Designation}").SetFontSize(10));
            leftCell.Add(new Paragraph($"Mobile: {employee.MobileNo}").SetFontSize(10));
            leftCell.Add(new Paragraph($"Email: {employee.Email}").SetFontSize(10));
            
            // Right column - Photo and QR Code
            var rightCell = new Cell();
            rightCell.Add(new Paragraph("Photo").SetFontSize(10).SetTextAlignment(TextAlignment.CENTER));
            rightCell.Add(new Paragraph("QR Code").SetFontSize(10).SetTextAlignment(TextAlignment.CENTER));
            
            table.AddCell(leftCell);
            table.AddCell(rightCell);
            
            document.Add(table);
        }

        private string GenerateQRCode(string data)
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            var imageBytes = qrCode.GetGraphic(20);
            return "data:image/png;base64," + Convert.ToBase64String(imageBytes);
        }

        private string GenerateIdCardHtml(Employees employee, string qrCodeImage)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>ID Card - {employee.Name}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
        .id-card {{ 
            width: 400px; height: 250px; border: 2px solid #333; 
            border-radius: 10px; padding: 20px; margin: 20px auto;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white; position: relative; overflow: hidden;
        }}
        .header {{ text-align: center; margin-bottom: 20px; }}
        .company-name {{ font-size: 18px; font-weight: bold; margin-bottom: 5px; }}
        .card-title {{ font-size: 14px; opacity: 0.9; }}
        .content {{ display: flex; justify-content: space-between; }}
        .left {{ flex: 1; }}
        .right {{ flex: 1; text-align: right; }}
        .photo {{ width: 80px; height: 80px; border-radius: 50%; border: 3px solid white; }}
        .qr-code {{ width: 60px; height: 60px; }}
        .field {{ margin-bottom: 8px; font-size: 12px; }}
        .label {{ font-weight: bold; }}
        .footer {{ position: absolute; bottom: 10px; left: 20px; right: 20px; text-align: center; font-size: 10px; opacity: 0.8; }}
    </style>
</head>
<body>
    <div class='id-card'>
        <div class='header'>
            <div class='company-name'>iCard System</div>
            <div class='card-title'>Employee Identity Card</div>
        </div>
        <div class='content'>
            <div class='left'>
                <div class='field'><span class='label'>ID:</span> {employee.EmployeeId}</div>
                <div class='field'><span class='label'>Name:</span> {employee.Name}</div>
                <div class='field'><span class='label'>Dept:</span> {employee.Department}</div>
                <div class='field'><span class='label'>Designation:</span> {employee.Designation}</div>
                <div class='field'><span class='label'>Mobile:</span> {employee.MobileNo}</div>
                <div class='field'><span class='label'>Email:</span> {employee.Email}</div>
            </div>
            <div class='right'>
                <img src='{(string.IsNullOrEmpty(employee.Image) ? "/favicon.ico" : employee.Image)}' class='photo' alt='Photo' />
                <div style='margin-top: 10px;'>
                    <img src='{qrCodeImage}' class='qr-code' alt='QR Code' />
                </div>
            </div>
        </div>
        <div class='footer'>
            Valid Till: {employee.ValidTill?.ToString("MMM yyyy") ?? "N/A"} | Issued: {employee.IDCardIssueDate?.ToString("MMM yyyy") ?? DateTime.Now.ToString("MMM yyyy")}
        </div>
    </div>
</body>
</html>";
        }
    }
}

