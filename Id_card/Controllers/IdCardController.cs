//using Id_card.Models;
//using Id_card.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace Id_card.Controllers
//{
//    public class IdCardController : Controller
//    {
//        private readonly ICardDbContext _context;
//        private readonly IdCardService _idCardService;
//        private readonly EmailService _emailService;
//        private readonly RazorViewToStringRenderer _renderer;

//        public IdCardController(ICardDbContext context, IdCardService idCardService, EmailService emailService, RazorViewToStringRenderer renderer)
//        {
//            _context = context;
//            _idCardService = idCardService;
//            _emailService = emailService;
//            _renderer = renderer;
//        }

//        // GET: /IdCard/Preview/5
//        public async Task<IActionResult> Preview(int id)
//        {
//            var userId = HttpContext.Session.GetInt32("UserId");
//            if (!userId.HasValue) return RedirectToAction("Login", "Users");

//            var employee = await _context.Employees.Include(e => e.Address).FirstOrDefaultAsync(e => e.EmployeeId == id);
//            if (employee == null) return NotFound();

//            var qrData = $"Employee ID: {employee.EmployeeId}\nName: {employee.Name}\nDepartment: {employee.Department}\nDesignation: {employee.Designation}\nEmail: {employee.Email}";
//            var qrCodeImage = _idCardService.GenerateQRCode(qrData);
//            ViewBag.QR = qrCodeImage;
//            return View(employee);
//        }

//        // GET: /IdCard/Pdf/5
//        public async Task<IActionResult> Pdf(int id)
//        {
//            var userId = HttpContext.Session.GetInt32("UserId");
//            if (!userId.HasValue) return RedirectToAction("Login", "Users");

//            var employee = await _context.Employees.Include(e => e.Address).FirstOrDefaultAsync(e => e.EmployeeId == id);
//            if (employee == null) return NotFound();

//            var qrData = $"Employee ID: {employee.EmployeeId}\nName: {employee.Name}\nDepartment: {employee.Department}\nDesignation: {employee.Designation}\nEmail: {employee.Email}";
//            var qrCodeImage = _idCardService.GenerateQRCode(qrData);
//            var bytes = _idCardService.BuildIdCardPdfBytes(employee, qrCodeImage);
//            return File(bytes, "application/pdf", $"IDCard_{employee.EmployeeId}_{employee.Name.Replace(" ", "_")}.pdf");
//        }

//        // POST: /IdCard/Email/5
//        [HttpPost]
//        public async Task<IActionResult> Email(int id)
//        {
//            var userId = HttpContext.Session.GetInt32("UserId");
//            if (!userId.HasValue) return RedirectToAction("Login", "Users");

//            var employee = await _context.Employees.Include(e => e.Address).FirstOrDefaultAsync(e => e.EmployeeId == id);
//            if (employee == null || string.IsNullOrEmpty(employee.Email)) return NotFound();

//            var qrData = $"Employee ID: {employee.EmployeeId}\nName: {employee.Name}\nDepartment: {employee.Department}\nDesignation: {employee.Designation}\nEmail: {employee.Email}";
//            var qrCodeImage = _idCardService.GenerateQRCode(qrData);

//            // Render the Razor view for email body with QR image in ViewData
//            // Resolve using relative path with extension to satisfy GetView fallback
//            var html = await _renderer.RenderViewToStringAsync(this, "~/Views/IdCard/Preview.cshtml", employee, new Dictionary<string, object>
//            {
//                { "QR", qrCodeImage }
//            });

//            // Attach PDF
//            var bytes = _idCardService.BuildIdCardPdfBytes(employee, qrCodeImage);
//            var tmpDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tmp");
//            if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);
//            var outPath = Path.Combine(tmpDir, $"IDCard_{employee.EmployeeId}.pdf");
//            await System.IO.File.WriteAllBytesAsync(outPath, bytes);

//            await _emailService.SendEmailAsync(employee.Email, "Your ID Card", html, outPath);

//            employee.SentOnMailStatus = true;
//            _context.Update(employee);
//            await _context.SaveChangesAsync();

//            TempData["Message"] = "ID Card sent to employee's email.";
//            return RedirectToAction("Index", "Employees");
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
    public class IdCardController : Controller
    {
        private readonly ICardDbContext _context;
        private readonly IdCardService _idCardService;
        private readonly EmailService _emailService;
        private readonly RazorViewToStringRenderer _renderer;

        public IdCardController(ICardDbContext context, IdCardService idCardService, EmailService emailService, RazorViewToStringRenderer renderer)
        {
            _context = context;
            _idCardService = idCardService;
            _emailService = emailService;
            _renderer = renderer;
        }

        // GET: /IdCard/Preview/5
        public async Task<IActionResult> Preview(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Users");

            var employee = await _context.Employees
                .Include(e => e.Address)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            // ✅ Include ALL employee data in QR code
            var qrData = BuildFullQRData(employee);
            var qrCodeImage = _idCardService.GenerateQRCode(qrData);

            ViewBag.QR = qrCodeImage;
            return View(employee);
        }

        // GET: /IdCard/Pdf/5
        public async Task<IActionResult> Pdf(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Users");

            var employee = await _context.Employees
                .Include(e => e.Address)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            // ✅ Same QR content for PDF
            var qrData = BuildFullQRData(employee);
            var qrCodeImage = _idCardService.GenerateQRCode(qrData);

            var bytes = _idCardService.BuildIdCardPdfBytes(employee, qrCodeImage);
            return File(bytes, "application/pdf", $"IDCard_{employee.EmployeeId}_{employee.Name.Replace(" ", "_")}.pdf");
        }

        // POST: /IdCard/Email/5
        [HttpPost]
        public async Task<IActionResult> Email(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Users");

            var employee = await _context.Employees
                .Include(e => e.Address)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null || string.IsNullOrEmpty(employee.Email))
                return NotFound();

            // ✅ Full data QR for email
            var qrData = BuildFullQRData(employee);
            var qrCodeImage = _idCardService.GenerateQRCode(qrData);

            // ✅ Render email body with embedded QR
            var html = await _renderer.RenderViewToStringAsync(this, "~/Views/IdCard/Preview.cshtml", employee, new Dictionary<string, object>
            {
                { "QR", qrCodeImage }
            });

            // ✅ Attach PDF version of ID card
            var bytes = _idCardService.BuildIdCardPdfBytes(employee, qrCodeImage);
            var tmpDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tmp");
            if (!Directory.Exists(tmpDir))
                Directory.CreateDirectory(tmpDir);

            var outPath = Path.Combine(tmpDir, $"IDCard_{employee.EmployeeId}.pdf");
            await System.IO.File.WriteAllBytesAsync(outPath, bytes);

            await _emailService.SendEmailAsync(employee.Email, "Your ID Card", html, outPath);

            // ✅ Update mail sent status
            employee.SentOnMailStatus = true;
            _context.Update(employee);
            await _context.SaveChangesAsync();

            TempData["Message"] = "ID Card sent to employee's email.";
            return RedirectToAction("Index", "Employees");
        }

        // 🧩 Helper Method: Build Full QR Data String
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



