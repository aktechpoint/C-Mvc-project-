using Id_card.Models;
using Id_card.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace Id_card.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly ICardDbContext _context;
        private readonly EmailService _emailService;
        private readonly IdCardService _idCardService;

        public EmployeesController(ICardDbContext context, EmailService emailService, IdCardService idCardService)
        {
            _context = context;
            _emailService = emailService;
            _idCardService = idCardService;
        }

        // GET: Employees List with pagination and filters
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string search = "", 
            string department = "", string designation = "", DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            var query = _context.Employees
                .Include(e => e.Address)
                .Include(e => e.CreatedUser)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.Name.Contains(search) || 
                    e.MobileNo.Contains(search) || 
                    e.EmployeeId.ToString().Contains(search));
            }

            if (!string.IsNullOrEmpty(department))
                query = query.Where(e => e.Department == department);

            if (!string.IsNullOrEmpty(designation))
                query = query.Where(e => e.Designation == designation);

            if (dateFrom.HasValue)
                query = query.Where(e => e.DateOfJoining >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(e => e.DateOfJoining <= dateTo.Value);

            var totalCount = await query.CountAsync();
            var employees = await query
                .OrderByDescending(e => e.EmployeeId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Search = search;
            ViewBag.Department = department;
            ViewBag.Designation = designation;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            // Get unique departments and designations for filter dropdowns
            ViewBag.Departments = await _context.Employees.Select(e => e.Department).Distinct().ToListAsync();
            ViewBag.Designations = await _context.Employees.Select(e => e.Designation).Distinct().ToListAsync();

            return View(employees);
        }

        // GET: Add Employee
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            return View(new Employees());
        }

        // POST: Add Employee
        [HttpPost]
        public async Task<IActionResult> Create(Employees employee, IFormFile? imageFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            if (!ModelState.IsValid)
                return View(employee);

            employee.CreatedBy = userId.Value;
            employee.CardCreateDate = DateTime.Now;

            if (imageFile != null && imageFile.Length > 0)
            {
                employee.Image = await SaveImage(imageFile);
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Employee added successfully.";
            return RedirectToAction("Index");
        }

        // GET: Bulk Upload
        public IActionResult BulkUpload()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            return View();
        }

        // POST: Bulk Upload
        [HttpPost]
        public async Task<IActionResult> BulkUpload(IFormFile excelFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            if (excelFile == null || excelFile.Length == 0)
            {
                ViewBag.Error = "Please select an Excel file.";
                return View();
            }

            try
            {
                using var stream = excelFile.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1); // Skip header

                var employees = new List<Employees>();
                foreach (var row in rows)
                {
                    var employee = new Employees
                    {
                        Name = row.Cell(1).GetString(),
                        FatherName = row.Cell(2).GetString(),
                        MotherName = row.Cell(3).GetString(),
                        DOB = row.Cell(4).GetDateTime(),
                        Department = row.Cell(5).GetString(),
                        Designation = row.Cell(6).GetString(),
                        DateOfJoining = row.Cell(7).GetDateTime(),
                        BloodGroup = row.Cell(8).GetString(),
                        MobileNo = row.Cell(9).GetString(),
                        Email = row.Cell(10).GetString(),
                        EmergencyContactName = row.Cell(11).GetString(),
                        EmergencyContactNo = row.Cell(12).GetString(),
                        CreatedBy = userId.Value,
                        CardCreateDate = DateTime.Now
                    };

                    // Optional: Image path in column 13
                    var imgCell = row.Cell(13);
                    if (imgCell != null)
                    {
                        var pathOrUrl = imgCell.GetString();
                        if (!string.IsNullOrWhiteSpace(pathOrUrl))
                        {
                            employee.Image = pathOrUrl.Trim();
                        }
                    }

                    employees.Add(employee);
                }

                _context.Employees.AddRange(employees);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"Successfully imported {employees.Count} employees.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error importing file: {ex.Message}";
                return View();
            }
        }

        // Generate ID Card PDF and optionally email
        public async Task<IActionResult> GenerateIdCard(int id, bool sendEmail = false)
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
            var qrCodeImage = _idCardService.GenerateQRCode(qrData);

            // Generate ID Card PDF bytes
            var pdfBytes = _idCardService.BuildIdCardPdfBytes(employee, qrCodeImage);

            if (sendEmail && !string.IsNullOrEmpty(employee.Email))
            {
                try
                {
                    var tmpDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tmp");
                    if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);
                    var outPath = Path.Combine(tmpDir, $"IDCard_{employee.EmployeeId}.pdf");
                    await System.IO.File.WriteAllBytesAsync(outPath, pdfBytes);

                    await _emailService.SendEmailAsync(employee.Email, "Your ID Card", "Please find attached your ID card.", outPath);
                    employee.SentOnMailStatus = true;
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "ID Card sent to employee's email.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Failed to send email: {ex.Message}";
                }
            }

            return File(pdfBytes, "application/pdf", $"IDCard_{employee.EmployeeId}.pdf");
        }

        // Bulk Generate ID Cards
        [HttpPost]
        public async Task<IActionResult> BulkGenerateIdCards(int[] employeeIds, bool sendEmails = false)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            var employees = await _context.Employees
                .Include(e => e.Address)
                .Where(e => employeeIds.Contains(e.EmployeeId))
                .ToListAsync();

            var successCount = 0;
            var emailCount = 0;

            foreach (var employee in employees)
            {
                try
                {
                    var qrData = $"Employee ID: {employee.EmployeeId}\nName: {employee.Name}\nDepartment: {employee.Department}\nDesignation: {employee.Designation}\nEmail: {employee.Email}";
                    var qrCodeImage = _idCardService.GenerateQRCode(qrData);
                    var idCardHtml = _idCardService.BuildIdCardHtml(employee, qrCodeImage);

                    if (sendEmails && !string.IsNullOrEmpty(employee.Email))
                    {
                        await _emailService.SendEmailAsync(employee.Email, "Your ID Card", idCardHtml);
                        employee.SentOnMailStatus = true;
                        emailCount++;
                    }

                    employee.PrintedStatus = true;
                    successCount++;
                }
                catch (Exception ex)
                {
                    // Log error but continue with other employees
                    continue;
                }
            }

            _context.UpdateRange(employees);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Generated {successCount} ID cards. Sent {emailCount} emails.";
            return RedirectToAction("Index");
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            
            var fileName = $"emp_{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(uploads, fileName);
            
            using (var stream = System.IO.File.Create(filePath))
            {
                await imageFile.CopyToAsync(stream);
            }
            
            return $"/uploads/{fileName}";
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
            //            return $@"
            //<!DOCTYPE html>
            //<html>
            //<head>
            //    <title>ID Card - {employee.Name}</title>
            //    <style>
            //        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
            //        .id-card {{ 
            //            width: 400px; height: 250px; border: 2px solid #333; 
            //            border-radius: 10px; padding: 20px; margin: 20px auto;
            //            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            //            color: white; position: relative; overflow: hidden;
            //        }}
            //        .header {{ text-align: center; margin-bottom: 20px; }}
            //        .company-name {{ font-size: 18px; font-weight: bold; margin-bottom: 5px; }}
            //        .card-title {{ font-size: 14px; opacity: 0.9; }}
            //        .content {{ display: flex; justify-content: space-between; }}
            //        .left {{ flex: 1; }}
            //        .right {{ flex: 1; text-align: right; }}
            //        .photo {{ width: 80px; height: 80px; border-radius: 50%; border: 3px solid white; }}
            //        .qr-code {{ width: 60px; height: 60px; }}
            //        .field {{ margin-bottom: 8px; font-size: 12px; }}
            //        .label {{ font-weight: bold; }}
            //        .footer {{ position: absolute; bottom: 10px; left: 20px; right: 20px; text-align: center; font-size: 10px; opacity: 0.8; }}
            //    </style>
            //</head>
            //<body>
            //    <div class='id-card'>
            //        <div class='header'>
            //            <div class='company-name'>iCard System</div>
            //            <div class='card-title'>Employee Identity Card</div>
            //        </div>
            //        <div class='content'>
            //            <div class='left'>
            //                <div class='field'><span class='label'>ID:</span> {employee.EmployeeId}</div>
            //                <div class='field'><span class='label'>Name:</span> {employee.Name}</div>
            //                <div class='field'><span class='label'>Dept:</span> {employee.Department}</div>
            //                <div class='field'><span class='label'>Designation:</span> {employee.Designation}</div>
            //                <div class='field'><span class='label'>Mobile:</span> {employee.MobileNo}</div>
            //                <div class='field'><span class='label'>Email:</span> {employee.Email}</div>
            //                <div class='field'><span class='label'>Address:</span> {(employee.Address == null ? "" : ($"{employee.Address.HouseNo} {employee.Address.Street}, {employee.Address.City}, {employee.Address.State}, {employee.Address.Country} {employee.Address.Pincode}").Replace("  ", " ").Trim())}</div>
            //            </div>
            //            <div class='right'>
            //                <img src='{(string.IsNullOrEmpty(employee.Image) ? "/favicon.ico" : employee.Image)}' class='photo' alt='Photo' />
            //                <div style='margin-top: 10px;'>
            //                    <img src='{qrCodeImage}' class='qr-code' alt='QR Code' />
            //                </div>
            //            </div>
            //        </div>
            //        <div class='footer'>
            //            Valid Till: {employee.ValidTill?.ToString("MMM yyyy") ?? "N/A"} | Issued: {employee.IDCardIssueDate?.ToString("MMM yyyy") ?? DateTime.Now.ToString("MMM yyyy")}
            //        </div>
            //    </div>
            //</body>
            //</html>";
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>ID Card - {employee.Name}</title>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            margin: 0;
                            padding: 40px;
                            background: #f4f6f8;
                        }}
                        .aadhaar-card {{
                            width: 540px;
                            border-radius: 12px;
                            background: #ffffff;
                            box-shadow: 0 10px 25px rgba(0,0,0,0.12);
                            border: 1px solid #e7e7e7;
                            overflow: hidden;
                            margin: 0 auto;
                        }}
                        .top-band {{ display:flex; height: 6px; }}
                        .band {{ flex:1; }}
                        .band.saffron {{ background:#ff9933; }}
                        .band.white {{ background:#ffffff; }}
                        .band.green {{ background:#138808; }}

                        .card-body {{ display:flex; position:relative; padding:16px; }}
                        .card-left {{
                            width: 160px;
                            padding: 8px 12px;
                            border-right: 1px dashed #d8d8d8;
                            display:flex;
                            flex-direction:column;
                            align-items:center;
                        }}
                        .emblem-wrap {{ text-align:center; margin-bottom: 10px; }}
                        .emblem-circle {{
                            width:36px;
                            height:36px;
                            border-radius:50%;
                            background:#222;
                            margin:0 auto;
                            position:relative;
                        }}
                        .emblem-dot {{
                            position:absolute;
                            width:8px;
                            height:8px;
                            border-radius:50%;
                            background:#fff;
                            top:50%;
                            left:50%;
                            transform: translate(-50%,-50%);
                        }}
                        .org-name {{
                            font-weight:700;
                            font-size:14px;
                            margin-top:8px;
                            color:#333;
                            letter-spacing:.3px;
                        }}
                        .org-sub {{
                            font-size:11px;
                            color:#777;
                        }}
                        .profile-photo {{
                            width:112px;
                            height:112px;
                            border-radius:6px;
                            object-fit:cover;
                            border: 2px solid #f0f0f0;
                        }}

                        .card-right {{ flex:1; padding: 4px 16px; }}
                        .field-row {{
                            display:flex;
                            align-items:center;
                            justify-content:space-between;
                            padding:6px 0;
                            border-bottom:1px dashed #eee;
                        }}
                        .field-row:last-child {{ border-bottom:none; }}
                        .key {{
                            color:#6b7280;
                            font-size:12px;
                            text-transform:uppercase;
                            letter-spacing:.5px;
                        }}
                        .val {{
                            color:#111827;
                            font-size:14px;
                            font-weight:600;
                        }}
                        .address .val {{ display:block; text-align:right; }}
                        .meta {{
                            display:flex;
                            gap:16px;
                            margin-top:10px;
                            color:#555;
                            font-size:12px;
                        }}

                        .qr-wrap {{
                            right:12px;
                            bottom:12px;
                            text-align:center;
                            margin-top:12px;
                        }}
                        .qr-code {{
                            width:94px;
                            height:94px;
                            border:1px solid #eee;
                            padding:6px;
                            background:#fff;
                            border-radius:8px;
                        }}
                        .qr-caption {{
                            font-size:10px;
                            color:#666;
                            margin-top:4px;
                        }}

                        .bottom-bar {{
                            background:#f9fafb;
                            border-top:1px solid #eee;
                            padding:6px 12px;
                            text-align:center;
                        }}
                        .bar-text {{
                            font-size:12px;
                            color:#6b7280;
                            letter-spacing:.2px;
                        }}
                    </style>
                </head>
                <body>
                    <div class='aadhaar-card'>
                        <div class='top-band'>
                            <span class='band saffron'></span>
                            <span class='band white'></span>
                            <span class='band green'></span>
                        </div>

                        <div class='card-body'>
                            <div class='card-left'>
                                <div class='emblem-wrap'>
                                    <div class='emblem-circle'>
                                        <span class='emblem-dot'></span>
                                    </div>
                                    <div class='org-name'>iCard System</div>
                                    <div class='org-sub'>Government Authorized Identity</div>
                                </div>

                                <img src='{(string.IsNullOrEmpty(employee.Image) ? "/favicon.ico" : employee.Image)}' 
                                     class='profile-photo' alt='Employee Photo' />

                                <div class='qr-wrap'>
                                    <img src='{qrCodeImage}' class='qr-code' alt='QR Code' />
                                    <div class='qr-caption'>Scan for verification</div>
                                </div>
                            </div>

                            <div class='card-right'>
                                <div class='field-row'>
                                    <span class='key'>Employee ID</span>
                                    <span class='val'>{employee.EmployeeId}</span>
                                </div>
                                <div class='field-row'>
                                    <span class='key'>Name</span>
                                    <span class='val'>{employee.Name}</span>
                                </div>
                                <div class='field-row'>
                                    <span class='key'>Department</span>
                                    <span class='val'>{employee.Department}</span>
                                </div>
                                <div class='field-row'>
                                    <span class='key'>Designation</span>
                                    <span class='val'>{employee.Designation}</span>
                                </div>
                                {(string.IsNullOrWhiteSpace(employee.BloodGroup) ? "" :
                                 $"<div class='field-row'><span class='key'>Blood Group</span><span class='val'>{employee.BloodGroup}</span></div>")}
                                <div class='field-row'>
                                    <span class='key'>Mobile</span>
                                    <span class='val'>{employee.MobileNo}</span>
                                </div>
                                <div class='field-row'>
                                    <span class='key'>Email</span>
                                    <span class='val'>{employee.Email}</span>
                                </div>
                                <div class='field-row address'>
                                    <span class='key'>Address</span>
                                    <span class='val'>{(employee.Address == null ? "" :
                                     ($"{employee.Address.HouseNo} {employee.Address.Street}, {employee.Address.City}, {employee.Address.State}, {employee.Address.Country} {employee.Address.Pincode}")
                                     .Replace("  ", " ").Trim())}</span>
                                </div>
                                <div class='meta'>
                                    <span>Issued: {(employee.IDCardIssueDate?.ToString("dd MMM yyyy") ?? DateTime.Now.ToString("dd MMM yyyy"))}</span>
                                    <span>Valid Till: {(employee.ValidTill?.ToString("dd MMM yyyy") ?? "N/A")}</span>
                                </div>
                            </div>
                        </div>

                        <div class='bottom-bar'>
                            <div class='bar-text'>Toll-free: 1800-000-000 | www.icard.example</div>
                        </div>
                    </div>
                </body>
                </html>";


        }

        private byte[] GenerateIdCardPdfBytes(Employees employee, string qrCodeImage)
        {
            // Deprecated: logic moved to IdCardService; keeping method for compatibility if referenced elsewhere
            var service = new Id_card.Services.IdCardService();
            return service.BuildIdCardPdfBytes(employee, qrCodeImage);
        }

        // GET: Edit Employee
        public async Task<IActionResult> Edit(int id)
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

            // Ensure nested Address is not null for the edit form binding
            if (employee.Address == null)
            {
                employee.Address = new Address();
            }

            return View(employee);
        }

        // POST: Edit Employee
        [HttpPost]
        public async Task<IActionResult> Edit(Employees model, IFormFile? imageFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Users");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var employee = await _context.Employees
                .Include(e => e.Address)
                .FirstOrDefaultAsync(e => e.EmployeeId == model.EmployeeId);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("Index");
            }

            // Update scalar fields
            employee.Name = model.Name;
            employee.FatherName = model.FatherName;
            employee.MotherName = model.MotherName;
            employee.DOB = model.DOB;
            employee.Department = model.Department;
            employee.Designation = model.Designation;
            employee.DateOfJoining = model.DateOfJoining;
            employee.IDCardIssueDate = model.IDCardIssueDate;
            employee.ValidTill = model.ValidTill;
            employee.BloodGroup = model.BloodGroup;
            employee.MobileNo = model.MobileNo;
            employee.Email = model.Email;
            employee.EmergencyContactName = model.EmergencyContactName;
            employee.EmergencyContactNo = model.EmergencyContactNo;
            employee.IsActive = model.IsActive;
            employee.UpdatedAt = DateTime.Now;

            // Update image if provided
            if (imageFile != null && imageFile.Length > 0)
            {
                employee.Image = await SaveImage(imageFile);
            }

            // Update or create address
            if (employee.Address == null)
            {
                employee.Address = new Address();
            }
            employee.Address.HouseNo = model.Address?.HouseNo;
            employee.Address.Street = model.Address?.Street;
            employee.Address.City = model.Address?.City;
            employee.Address.State = model.Address?.State;
            employee.Address.Country = model.Address?.Country;
            employee.Address.Pincode = model.Address?.Pincode;
            employee.Address.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Employee updated successfully.";
            return RedirectToAction("Index");
        }
    }
}
