using Microsoft.AspNetCore.Mvc;
using Admin.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Admin.Controllers
{
    public class OrdersController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public OrdersController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // Hiển thị danh sách Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _firebaseService.GetCollectionAsync("orders");
            return View(orders);
        }

        // Trang Edit Order
        public async Task<IActionResult> Edit(string id)
        {
            // Lấy thông tin order từ Firestore
            var orders = await _firebaseService.GetCollectionAsync("orders");
            var selectedOrder = orders.FirstOrDefault(o => o["id"].ToString() == id);

            if (selectedOrder == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            return View(selectedOrder);
        }

        // Xử lý cập nhật thông tin Order
        [HttpPost]
        public async Task<IActionResult> Edit(string id, string order_status)
        {
            // Lấy thông tin order từ Firestore
            var orders = await _firebaseService.GetCollectionAsync("orders");
            var selectedOrder = orders.FirstOrDefault(o => o["id"].ToString() == id);

            if (selectedOrder == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            // Cập nhật trạng thái order
            selectedOrder["order_status"] = order_status;
            selectedOrder["order_placed"] = order_status == "placed";
            selectedOrder["order_confirmed"] = order_status == "confirmed";
            selectedOrder["order_on_delivery"] = order_status == "on_delivery";
            selectedOrder["order_delivered"] = order_status == "delivered";

            // Ghi lại thông tin vào Firestore
            await _firebaseService.UpdateDocumentAsync("orders", id, selectedOrder);

            TempData["Message"] = "Order has been updated successfully.";
            return RedirectToAction("Index");
        }

        // Cập nhật trạng thái Order
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string orderId, string newStatus)
        {
            var orders = await _firebaseService.GetCollectionAsync("orders");
            var selectedOrder = orders.FirstOrDefault(o => o["id"].ToString() == orderId);

            if (selectedOrder == null)
            {
                return NotFound("Order does not exist");
            }

            selectedOrder["order_status"] = newStatus;
            selectedOrder["order_placed"] = newStatus == "placed";
            selectedOrder["order_confirmed"] = newStatus == "confirmed";
            selectedOrder["order_on_delivery"] = newStatus == "on_delivery";
            selectedOrder["order_delivered"] = newStatus == "delivered";

            await _firebaseService.UpdateDocumentAsync("orders", orderId, selectedOrder);

            TempData["Message"] = "Order status has been updated";
            return RedirectToAction("Index");
        }

        // Xóa Order
        [HttpPost]
        public async Task<IActionResult> Delete(string orderId)
        {
            await _firebaseService.DeleteDocumentAsync("orders", orderId);

            TempData["Message"] = "Order has been deleted";
            return RedirectToAction("Index");
        }
    }
}
