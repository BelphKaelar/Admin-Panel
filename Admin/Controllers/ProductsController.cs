﻿using Microsoft.AspNetCore.Mvc;
using Admin.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Admin.Controllers
{
    public class ProductsController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public ProductsController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // Hiển thị danh sách sản phẩm
        public async Task<IActionResult> Index(string search = null)
        {
            var products = await _firebaseService.GetCollectionAsync("products");

            // Tìm kiếm sản phẩm theo tên hoặc danh mục
            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products
                    .Where(p =>
                        (p.ContainsKey("p_name") && p["p_name"].ToString().ToLower().Contains(search.ToLower())) ||
                        (p.ContainsKey("p_category") && p["p_category"].ToString().ToLower().Contains(search.ToLower()))
                    )
                    .ToList();
            }

            ViewData["SearchQuery"] = search;
            return View(products);
        }

        // Trang thêm sản phẩm
        public IActionResult Add()
        {
            var categories = new List<string>
            {
                "Laptop", "Gaming Laptop", "Headphones", "Monitor",
                "Speakers", "Mobile Phone", "Console",
                "Mouse & Keyboard", "Accessories"
            };

            ViewData["Categories"] = categories;
            return View();
        }

        // Thêm sản phẩm
        [HttpPost]
        public async Task<IActionResult> Add(string p_name, IEnumerable<IFormFile> p_imgs, string p_price, string p_quantity,
                                             string p_desc, string p_seller, string p_colors, string p_category, string p_subcategory)
        {
            if (string.IsNullOrWhiteSpace(p_name) || string.IsNullOrWhiteSpace(p_price) || string.IsNullOrWhiteSpace(p_quantity))
            {
                TempData["Error"] = "Please fill in all required fields!";
                return RedirectToAction(nameof(Add));
            }

            var imageUrls = new List<string>();
            if (p_imgs != null)
            {
                foreach (var img in p_imgs)
                {
                    string imageUrl = await _firebaseService.UploadImageAsync(img);
                    imageUrls.Add(imageUrl);
                }
            }

            var colorsList = !string.IsNullOrWhiteSpace(p_colors)
                ? p_colors.Split(',').Select(c => c.Trim()).ToList()
                : new List<string>();

            var productData = new Dictionary<string, object>
            {
                { "p_name", p_name },
                { "p_imgs", imageUrls },
                { "p_price", p_price },
                { "p_quantity", p_quantity },
                { "p_desc", p_desc },
                { "p_seller", p_seller },
                { "p_colors", colorsList },
                { "p_category", p_category },
                { "p_subcategory", p_subcategory },
                { "p_wishlist", new List<string>() },
                { "p_rating", "5.0" },
                { "is_featured", false }
            };

            try
            {
                await _firebaseService.AddDocumentAsync("products", productData);
                TempData["Message"] = "Product added successfully!";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Failed to add product: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Trang chỉnh sửa sản phẩm
        public async Task<IActionResult> Edit(string id)
        {
            var productData = (await _firebaseService.GetCollectionAsync("products"))
                .FirstOrDefault(p => p.ContainsKey("id") && p["id"].ToString() == id);

            if (productData == null)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction("Index");
            }

            var categories = new List<string>
            {
                "Laptop", "Gaming Laptop", "Headphones", "Monitor",
                "Speakers", "Mobile Phone", "Console",
                "Mouse & Keyboard", "Accessories"
            };

            ViewData["Categories"] = categories;
            return View(productData);
        }

        // Cập nhật sản phẩm
        [HttpPost]
        public async Task<IActionResult> Edit(string id, string p_name, IEnumerable<IFormFile> p_imgs, string p_price, string p_quantity,
                                              string p_desc, string p_seller, string p_colors, string p_category, string p_subcategory)
        {
            var productData = (await _firebaseService.GetCollectionAsync("products"))
                .FirstOrDefault(p => p.ContainsKey("id") && p["id"].ToString() == id);

            if (productData == null)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction("Index");
            }

            if (productData.ContainsKey("p_imgs") && productData["p_imgs"] is List<object> existingImages)
            {
                var existingImageUrls = existingImages.Cast<string>().ToList();

                if (p_imgs != null)
                {
                    foreach (var img in p_imgs)
                    {
                        var imageUrl = await _firebaseService.UploadImageAsync(img);
                        existingImageUrls.Add(imageUrl);
                    }
                }

                productData["p_imgs"] = existingImageUrls;
            }

            productData["p_name"] = p_name;
            productData["p_price"] = p_price;
            productData["p_quantity"] = p_quantity;
            productData["p_desc"] = p_desc;
            productData["p_seller"] = p_seller;

            var colorsList = !string.IsNullOrWhiteSpace(p_colors)
                ? p_colors.Split(',').Select(c => c.Trim()).ToList()
                : productData.ContainsKey("p_colors") && productData["p_colors"] is IEnumerable<object> colors
                ? colors.Where(c => c is string).Cast<string>().ToList()
                : new List<string>();

            productData["p_colors"] = colorsList;
            productData["p_category"] = p_category;
            productData["p_subcategory"] = p_subcategory;

            try
            {
                await _firebaseService.UpdateDocumentAsync("products", id, productData);
                TempData["Message"] = "Product updated successfully!";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Failed to update product: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Xóa sản phẩm
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var productData = (await _firebaseService.GetCollectionAsync("products"))
                    .FirstOrDefault(p => p["id"].ToString() == id);

                if (productData == null)
                {
                    TempData["Error"] = "Product not found!";
                    return RedirectToAction("Index");
                }

                if (productData.ContainsKey("p_imgs") && productData["p_imgs"] is List<string> imageUrls)
                {
                    foreach (var imageUrl in imageUrls)
                    {
                        await _firebaseService.DeleteImageAsync(imageUrl);
                    }
                }

                await _firebaseService.DeleteDocumentAsync("products", id);
                TempData["Message"] = "Product deleted successfully!";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Failed to delete product: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
