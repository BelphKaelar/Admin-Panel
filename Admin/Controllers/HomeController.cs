using System.Diagnostics;
using Admin.Models;
using Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FirebaseService _firebaseService;

        public HomeController(ILogger<HomeController> logger, FirebaseService firebaseService)
        {
            _logger = logger;
            _firebaseService = firebaseService;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy dữ liệu từ Firestore
            var products = await _firebaseService.GetCollectionAsync("products");
            var orders = await _firebaseService.GetCollectionAsync("orders");
            var customers = await _firebaseService.GetCollectionAsync("users");

            // Tính toán dữ liệu cần hiển thị
            // Lấy các sản phẩm được đánh dấu là "is_featured = true"
            var featuredProducts = products.Where(p =>
                p.ContainsKey("is_featured") &&
                ((p["is_featured"] is bool && (bool)p["is_featured"]) ||
                 (p["is_featured"]?.ToString().ToLower() == "true"))
            ).ToList();

            var totalRevenue = orders.Sum(o => Convert.ToDecimal(o["total_amount"] ?? 0));
            var orderStatuses = new Dictionary<string, int>
            {
                { "placed", orders.Count(o => o["order_status"]?.ToString() == "placed") },
                { "confirmed", orders.Count(o => o["order_status"]?.ToString() == "confirmed") },
                { "on_delivery", orders.Count(o => o["order_status"]?.ToString() == "on_delivery") },
                { "delivered", orders.Count(o => o["order_status"]?.ToString() == "delivered") }
            };

            // Chuyển đổi `order_date` từ Firestore Timestamp sang DateTime
            var now = DateTime.UtcNow;
            var ordersByTime = new Dictionary<string, int>
            {
                { "Today", orders.Count(o => o.ContainsKey("order_date") &&
                                             o["order_date"] is Google.Cloud.Firestore.Timestamp timestamp &&
                                             timestamp.ToDateTime().Date == now.Date) },
                { "This Week", orders.Count(o => o.ContainsKey("order_date") &&
                                                 o["order_date"] is Google.Cloud.Firestore.Timestamp timestamp &&
                                                 timestamp.ToDateTime().Date >= now.AddDays(-7).Date) },
                { "This Month", orders.Count(o => o.ContainsKey("order_date") &&
                                                  o["order_date"] is Google.Cloud.Firestore.Timestamp timestamp &&
                                                  timestamp.ToDateTime().Month == now.Month &&
                                                  timestamp.ToDateTime().Year == now.Year) },
                { "This Year", orders.Count(o => o.ContainsKey("order_date") &&
                                                 o["order_date"] is Google.Cloud.Firestore.Timestamp timestamp &&
                                                 timestamp.ToDateTime().Year == now.Year) }
            };

            // Đếm số lượng khách hàng
            var customerCount = customers.Count;

            // Truyền dữ liệu vào ViewData
            ViewData["FeaturedProducts"] = featuredProducts;
            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["OrderStatuses"] = orderStatuses;
            ViewData["OrdersByTime"] = ordersByTime;
            ViewData["CustomerCount"] = customerCount;
            ViewData["CustomerList"] = customers;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
