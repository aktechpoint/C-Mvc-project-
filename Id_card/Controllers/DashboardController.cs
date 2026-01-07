//using Id_card.Models;
//using Id_card.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//// unused usings removed after refactor

//namespace Id_card.Controllers
//{
//    public class DashboardController : Controller
//    {
//        private readonly ICardDbContext _context;
//        private readonly EmailService _emailService;
//        private readonly IdCardService _idCardService;

//        public DashboardController(ICardDbContext context, EmailService emailService, IdCardService idCardService)
//        {
//            _context = context;
//            _emailService = emailService;
//            _idCardService = idCardService;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var userId = HttpContext.Session.GetInt32("UserId");
//            if (!userId.HasValue)
//            {
//                return RedirectToAction("Login", "Users");
//            }

//            // Get dashboard statistics
//            var totalEmployees = await _context.Employees.CountAsync();
//            var idCardsGenerated = await _context.Employees.CountAsync(e => e.PrintedStatus);
//            var activeEmployees = await _context.Employees.CountAsync(e => e.IsActive);
//            var inactiveEmployees = await _context.Employees.CountAsync(e => !e.IsActive);
//            var emailsSent = await _context.Employees.CountAsync(e => e.SentOnMailStatus);

//            ViewBag.TotalEmployees = totalEmployees;
//            ViewBag.IdCardsGenerated = idCardsGenerated;
//            ViewBag.ActiveEmployees = activeEmployees;
//            ViewBag.InactiveEmployees = inactiveEmployees;
//            ViewBag.EmailsSent = emailsSent;
//            ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "User";

//            return View();
//        }

//        // Employee Lists for each indicator
//        public async Task<IActionResult> EmployeeList(string type)
//        {
//            var userId = HttpContext.Session.GetInt32("UserId");
//            if (!userId.HasValue) return RedirectToAction("Login", "Users");

//            var query = _context.Employees.Include(e => e.Address).AsQueryable();

//            switch (type.ToLower())
//            {
//                case "total":
//                    break;
//                case "generated":
//                    query = query.Where(e => e.PrintedStatus);
//                    break;
//                case "active":
//                    query = query.Where(e => e.IsActive);
//                    break;
//                case "inactive":
//                    query = query.Where(e => !e.IsActive);
//                    break;
//                case "emailsent":
//                    query = query.Where(e => e.SentOnMailStatus);
//                    break;
//            }

//            var employees = await query.OrderByDescending(e => e.EmployeeId).ToListAsync();
//            ViewBag.ListType = type;
//            return View(employees);
//        }

//        // Generate ID Card as PDF
//        public async Task<IActionResult> GenerateIdCardPdf(int id)
//        {
//            var userId = HttpContext.Session.GetInt32("UserId");
//            if (!userId.HasValue) return RedirectToAction("Login", "Users");

//            var employee = await _context.Employees
//                .Include(e => e.Address)
//                .FirstOrDefaultAsync(e => e.EmployeeId == id);

//            if (employee == null)
//            {
//                TempData["Error"] = "Employee not found.";
//                return RedirectToAction("Index");
//            }

//            // Generate QR Code
//            var qrData = $"Employee ID: {employee.EmployeeId}\nName: {employee.Name}\nDepartment: {employee.Department}\nDesignation: {employee.Designation}\nEmail: {employee.Email}";
//            var qrCodeImage = _idCardService.GenerateQRCode(qrData);

//            // Build PDF using centralized service
//            var bytes = _idCardService.BuildIdCardPdfBytes(employee, qrCodeImage);
//            return File(bytes, "application/pdf", $"IDCard_{employee.EmployeeId}_{employee.Name.Replace(" ", "_")}.pdf");
//        }

//        // Send ID Card via Email (PDF attachment only)
//        public async Task<IActionResult> SendIdCardEmail(int id)
//        {
//            var userId = HttpContext.Session.GetInt32("UserId");
//            if (!userId.HasValue) return RedirectToAction("Login", "Users");

//            var employee = await _context.Employees
//                .Include(e => e.Address)
//                .FirstOrDefaultAsync(e => e.EmployeeId == id);

//            if (employee == null || string.IsNullOrEmpty(employee.Email))
//            {
//                TempData["Error"] = "Employee not found or email not available.";
//                return RedirectToAction("Index");
//            }

//            try
//            {
//                // Generate QR Code
//                var qrData = $"Employee ID: {employee.EmployeeId}\nName: {employee.Name}\nDepartment: {employee.Department}\nDesignation: {employee.Designation}\nEmail: {employee.Email}";
//                var qrCodeImage = _idCardService.GenerateQRCode(qrData);

//                // Build PDF using centralized service
//                var pdfBytes = _idCardService.BuildIdCardPdfBytes(employee, qrCodeImage);

//                // Save PDF to temp and attach
//                var tmpDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tmp");
//                if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);
//                var pdfPath = Path.Combine(tmpDir, $"IDCard_{employee.EmployeeId}_{employee.Name.Replace(" ", "_")}.pdf");
//                await System.IO.File.WriteAllBytesAsync(pdfPath, pdfBytes);

//                await _emailService.SendEmailAsync(employee.Email, "Your ID Card", "Please find attached your ID card.", pdfPath);

//                employee.SentOnMailStatus = true;
//                _context.Update(employee);
//                await _context.SaveChangesAsync();

//                TempData["Message"] = "ID Card sent successfully to employee's email.";
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = $"Failed to send email: {ex.Message}";
//            }

//            return RedirectToAction("Index");
//        }

//        // Get WhatsApp share link
//        public IActionResult GetWhatsAppLink(int id)
//        {
//            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeId == id);
//            if (employee == null)
//            {
//                return Json(new { success = false, message = "Employee not found" });
//            }

//            var message = $"ID Card for {employee.Name} (Employee ID: {employee.EmployeeId})";
//            var whatsappUrl = $"https://wa.me/?text={Uri.EscapeDataString(message)}";

//            return Json(new { success = true, url = whatsappUrl });
//        }

//        public IActionResult Logout()
//        {
//            HttpContext.Session.Clear();
//            return RedirectToAction("Login", "Users");
//        }


//    }
//}




using Id_card.Models;
using Id_card.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Id_card.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ICardDbContext _context;
        private readonly EmailService _emailService;
        private readonly IdCardService _idCardService;

        public DashboardController(ICardDbContext context, EmailService emailService, IdCardService idCardService)
        {
            _context = context;
            _emailService = emailService;
            _idCardService = idCardService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Users");
            }

            // Dashboard stats
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

        // Employee Lists
        public async Task<IActionResult> EmployeeList(string type)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            var query = _context.Employees.Include(e => e.Address).AsQueryable();

            switch (type.ToLower())
            {
                case "generated": query = query.Where(e => e.PrintedStatus); break;
                case "active": query = query.Where(e => e.IsActive); break;
                case "inactive": query = query.Where(e => !e.IsActive); break;
                case "emailsent": query = query.Where(e => e.SentOnMailStatus); break;
            }

            var employees = await query.OrderByDescending(e => e.EmployeeId).ToListAsync();
            ViewBag.ListType = type;
            return View(employees);
        }

        // ✅ Generate ID Card PDF (with all data in QR)
        public async Task<IActionResult> GenerateIdCardPdf(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Users");

            var employee = await _context.Employees
                .Include(e => e.Address)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("Index");
            }

            // ✅ Build full QR content
            var qrData = BuildFullQRData(employee);
            var qrCodeImage = _idCardService.GenerateQRCode(qrData);

            // Generate PDF
            var bytes = _idCardService.BuildIdCardPdfBytes(employee, qrCodeImage);
            return File(bytes, "application/pdf", $"IDCard_{employee.EmployeeId}_{employee.Name.Replace(" ", "_")}.pdf");
        }

        // ✅ Send ID Card via Email (PDF with full QR)
        public async Task<IActionResult> SendIdCardEmail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Users");

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
                // ✅ Generate QR with all data
                var qrData = BuildFullQRData(employee);
                var qrCodeImage = _idCardService.GenerateQRCode(qrData);

                // Build PDF
                var pdfBytes = _idCardService.BuildIdCardPdfBytes(employee, qrCodeImage);

                // Save and attach
                var tmpDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tmp");
                if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);

                var pdfPath = Path.Combine(tmpDir, $"IDCard_{employee.EmployeeId}_{employee.Name.Replace(" ", "_")}.pdf");
                await System.IO.File.WriteAllBytesAsync(pdfPath, pdfBytes);

                await _emailService.SendEmailAsync(
                    employee.Email,
                    "Your ID Card",
                    "Please find attached your official ID card.",
                    pdfPath
                );

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

        // ✅ WhatsApp share link
        public IActionResult GetWhatsAppLink(int id)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeId == id);
            if (employee == null)
                return Json(new { success = false, message = "Employee not found" });

            var message = $"ID Card for {employee.Name} (Employee ID: {employee.EmployeeId})";
            var whatsappUrl = $"https://wa.me/?text={Uri.EscapeDataString(message)}";

            return Json(new { success = true, url = whatsappUrl });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Users");
        }

        // 🧩 Helper Method: Build QR Data with all details
        private string BuildFullQRData(Employees employee)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Employee ID: {employee.EmployeeId}");
            sb.AppendLine($"Name: {employee.Name}");
            sb.AppendLine($"Department: {employee.Department}");
            sb.AppendLine($"Designation: {employee.Designation}");
            sb.AppendLine($"Blood Group: {employee.BloodGroup}");
            sb.AppendLine($"Mobile: {employee.MobileNo}");
            sb.AppendLine($"Email: {employee.Email}");

            if (employee.Address != null)
            {
                sb.AppendLine("Address:");
                sb.AppendLine($"{employee.Address.HouseNo} {employee.Address.Street},");
                sb.AppendLine($"{employee.Address.City}, {employee.Address.State},");
                sb.AppendLine($"{employee.Address.Country} - {employee.Address.Pincode}");
            }

            sb.AppendLine($"Issued: {employee.IDCardIssueDate?.ToString("dd MMM yyyy") ?? DateTime.Now.ToString("dd MMM yyyy")}");
            sb.AppendLine($"Valid Till: {employee.ValidTill?.ToString("dd MMM yyyy") ?? "N/A"}");
            return sb.ToString();
        }
    }
}
