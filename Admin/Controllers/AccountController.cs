using Microsoft.AspNetCore.Mvc;
using Admin.Services;
using Microsoft.AspNetCore.Http; // Để sử dụng Session
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public AccountController()
        {
            _firebaseService = new FirebaseService(); // Sử dụng FirebaseService đã có
        }

        // Trang đăng nhập (GET)
        public IActionResult Login()
        {
            // Kiểm tra nếu đã đăng nhập (Session có UserEmail)
            if (HttpContext.Session.GetString("AdminEmail") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // Xử lý đăng nhập (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                // Lấy dữ liệu admin từ Firestore
                var admins = await _firebaseService.GetCollectionAsync("admin");
                var admin = admins.Find(a =>
                    a["admin_email"].ToString() == email &&
                    a["admin_password"].ToString() == password
                );

                if (admin == null)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View();
                }

                // Nếu login thành công, lưu thông tin admin vào Session
                HttpContext.Session.SetString("AdminId", admin["admin_id"].ToString());
                HttpContext.Session.SetString("AdminName", admin["admin_name"].ToString());
                HttpContext.Session.SetString("AdminEmail", admin["admin_email"].ToString());

                // Chuyển hướng đến Dashboard hoặc Home
                return RedirectToAction("Index", "Home");
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", $"Login failed: {ex.Message}");
                return View();
            }
        }

        // Đăng xuất
        public IActionResult Logout()
        {
            // Xóa Session
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account"); // Quay lại trang đăng nhập
        }
    }
}
