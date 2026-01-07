using Id_card.Models;
using Id_card.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Id_card.Controllers
{
    public class UsersController : Controller
    {
        private readonly ICardDbContext _context;
        private readonly EmailService _emailService;
        private readonly PasswordHasher<Users> _hasher;

        public UsersController(ICardDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
            _hasher = new PasswordHasher<Users>();
        }

        // GET: Login
        public IActionResult Login()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and Password are required.";
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                ViewBag.Error = "Invalid Email or Password.";
                return View();
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Success)
            {
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserRole", user.Role ?? "User");

                user.LastLogin = DateTime.Now;
                _context.Update(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Invalid Email or Password.";
            return View();
        }

        // GET: Register (Admin only)
        public IActionResult Register() => View();

        //[HttpPost]
        //public async Task<IActionResult> Register(Users user)
        //{
        //    var currentRole = HttpContext.Session.GetString("UserRole");
        //    if (currentRole != "Admin") return Unauthorized();

        //    if (!ModelState.IsValid || string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.Email))
        //        return View(user);

        //    user.PasswordHash = _hasher.HashPassword(user, user.PasswordHash);
        //    user.IsActive = false; // pending OTP verification

        //    _context.Users.Add(user);
        //    await _context.SaveChangesAsync();

        //    // Generate OTP
        //    var otp = new Random().Next(100000, 999999).ToString();
        //    HttpContext.Session.SetString("OTP", otp);
        //    HttpContext.Session.SetString("PendingEmail", user.Email);

        //    try
        //    {
        //        await _emailService.SendEmailAsync(user.Email, "OTP Verification", $"Your OTP is {otp}");
        //        TempData["Message"] = "OTP sent to user's email. Enter OTP to complete registration.";
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        ViewBag.Error = ex.Message;
        //        return View(user);
        //    }
        //    return RedirectToAction("VerifyOTP", new { email = user.Email });
        //}

        [HttpPost]
        public async Task<IActionResult> Register(Users user)
        {
            var currentRole = HttpContext.Session.GetString("UserRole");
            if (currentRole != "Admin") return Unauthorized();

            if (!ModelState.IsValid || string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.Email))
                return View(user);

            // ✅ Check for duplicate email before saving
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ViewBag.Error = "Email already exists. Please use a different email.";
                return View(user);
            }

            user.PasswordHash = _hasher.HashPassword(user, user.PasswordHash);
            user.IsActive = false; // pending OTP verification

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("PendingEmail", user.Email);

            try
            {
                await _emailService.SendEmailAsync(user.Email, "OTP Verification", $"Your OTP is {otp}");
                TempData["Message"] = "OTP sent to user's email. Enter OTP to complete registration.";
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.Error = ex.Message;
                return View(user);
            }

            return RedirectToAction("VerifyOTP", new { email = user.Email });
        }


        // GET: Verify OTP
        [HttpGet]
        public IActionResult VerifyOTP(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOTP(string email, string otp)
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");
            var pendingEmail = HttpContext.Session.GetString("PendingEmail");

            var normalizedPostedEmail = (email ?? string.Empty).Trim();
            var normalizedPendingEmail = (pendingEmail ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(otp) || otp.Trim() != (sessionOtp ?? string.Empty).Trim() ||
                !string.Equals(normalizedPostedEmail, normalizedPendingEmail, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Invalid OTP.";
                ViewBag.Email = email; // preserve email for the view's hidden field
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "User not found.";
                ViewBag.Email = email;
                return View();
            }

            user.IsActive = true;
            _context.Update(user);
            await _context.SaveChangesAsync();

            // Auto sign-in and redirect to EditProfile
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserRole", user.Role ?? "User");

            HttpContext.Session.Remove("OTP");
            HttpContext.Session.Remove("PendingEmail");

            TempData["Message"] = "User registration successful.";
            return RedirectToAction("EditProfile");
        }

        // GET: Forgot Password
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Email is required.";
                return View();
            }

            // Fetch only necessary fields
            var user = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => new
                {
                    u.UserId,
                    u.Email,
                    u.PasswordHash,
                    u.IsActive
                })
                .FirstOrDefaultAsync();

            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                ViewBag.Error = "User not found.";
                return View();
            }

            // OTP logic
            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("PendingEmail", email);

            try
            {
                await _emailService.SendEmailAsync(email, "Password Reset OTP", $"Your OTP is {otp}");
                TempData["Message"] = "OTP sent to your email.";
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }

            return RedirectToAction("ResetPassword", new { email });
        }



        // GET: Reset Password
        public IActionResult ResetPassword(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string otp, string newPassword)
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");
            var pendingEmail = HttpContext.Session.GetString("PendingEmail");

            var normalizedPostedEmail = (email ?? string.Empty).Trim();
            var normalizedPendingEmail = (pendingEmail ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(otp) || otp.Trim() != (sessionOtp ?? string.Empty).Trim() ||
                !string.Equals(normalizedPostedEmail, normalizedPendingEmail, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Invalid OTP.";
                ViewBag.Email = email; // preserve email for the view's hidden field
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "User not found.";
                ViewBag.Email = email;
                return View();
            }

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            _context.Update(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("OTP");
            HttpContext.Session.Remove("PendingEmail");

            TempData["Message"] = "Password reset successful.";
            return RedirectToAction("Login");
        }

        // GET: Change Password
        public IActionResult ChangePassword() => View();

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                ViewBag.Error = "User not found.";
                return View();
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword);
            if (result != PasswordVerificationResult.Success)
            {
                ViewBag.Error = "Old password is incorrect.";
                return View();
            }

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Password changed successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: Profile (read-only card)
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        // GET: Edit Profile (form)
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        // POST: Edit Profile
        [HttpPost]
        public async Task<IActionResult> EditProfile(Users model, IFormFile? imageFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login");

            var user = await _context.Users.Include(u => u.Address).FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return RedirectToAction("Login");

            // Ignore validation for fields not part of this form (e.g., PasswordHash)
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Address.AddressId");

            user.FullName = model.FullName;
            user.MobileNo = model.MobileNo;
            user.IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            user.UpdatedAt = DateTime.Now;

            if (user.Address == null)
            {
                user.Address = new Address();
                _context.Entry(user.Address).State = EntityState.Added;
            }

            user.Address.HouseNo = model.Address?.HouseNo;
            user.Address.Street = model.Address?.Street;
            user.Address.City = model.Address?.City;
            user.Address.State = model.Address?.State;
            user.Address.Country = model.Address?.Country;
            user.Address.Pincode = model.Address?.Pincode;

            // Ensure EF tracks changes to Address if it already exists
            if (user.Address.AddressId != 0)
            {
                _context.Entry(user.Address).State = EntityState.Modified;
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var fileName = $"user_{user.UserId}_{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(uploads, fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await imageFile.CopyToAsync(stream);
                }
                user.Image = $"/uploads/{fileName}";
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Profile updated.";
            return RedirectToAction("Profile");
        }
    }
}
