using Microsoft.AspNetCore.Mvc;
using Admin.Services;

namespace Admin.Controllers
{
    public class CustomerController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public CustomerController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // Hiển thị danh sách khách hàng
        public async Task<IActionResult> Index(string search = null)
        {
            var customers = await _firebaseService.GetCollectionAsync("users");

            // Lọc khách hàng theo tên hoặc email
            if (!string.IsNullOrWhiteSpace(search))
            {
                customers = customers
                    .Where(c =>
                        (c.ContainsKey("name") && c["name"].ToString().ToLower().Contains(search.ToLower())) ||
                        (c.ContainsKey("email") && c["email"].ToString().ToLower().Contains(search.ToLower()))
                    )
                    .ToList();
            }

            ViewData["SearchQuery"] = search;
            return View(customers);
        }
       
        // Trang chỉnh sửa khách hàng
        public async Task<IActionResult> Edit(string id)
        {
            var customerData = (await _firebaseService.GetCollectionAsync("users"))
                .FirstOrDefault(c => c.ContainsKey("id") && c["id"].ToString() == id);

            if (customerData == null)
            {
                TempData["Error"] = "Customer not found!";
                return RedirectToAction("Index");
            }

            return View(customerData);
        }

        // Cập nhật khách hàng
        [HttpPost]
        public async Task<IActionResult> Edit(string id, string name, string email, string phone, string address, string province, string city)
        {
            var customerData = (await _firebaseService.GetCollectionAsync("users"))
                .FirstOrDefault(c => c.ContainsKey("id") && c["id"].ToString() == id);

            if (customerData == null)
            {
                TempData["Error"] = "Customer not found!";
                return RedirectToAction("Index");
            }

            // Update fields
            customerData["name"] = name;
            customerData["email"] = email;
            customerData["phone"] = phone;
            customerData["address"] = address;
            customerData["province"] = province;
            customerData["city"] = city;

            try
            {
                await _firebaseService.UpdateDocumentAsync("users", id, customerData);
                TempData["Message"] = "Customer updated successfully!";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Failed to update customer: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Xóa khách hàng
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var customerData = (await _firebaseService.GetCollectionAsync("users"))
                    .FirstOrDefault(c => c["id"].ToString() == id);

                if (customerData == null)
                {
                    TempData["Error"] = "Customer not found!";
                    return RedirectToAction("Index");
                }

                await _firebaseService.DeleteDocumentAsync("users", id);
                TempData["Message"] = "Customer deleted successfully!";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Failed to delete customer: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
