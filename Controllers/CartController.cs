using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ĐỒ_ÁN.Models;

namespace ĐỒ_ÁN.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart

        QL_VanPhongPhamEntities data = new QL_VanPhongPhamEntities();

        // Helper để lấy MaKH
        private int? GetMaKH()
        {
            if (Session["UserId"] != null)
                return int.Parse(Session["UserId"].ToString());

            var cookie = Request.Cookies["UserLogin"];
            if (cookie != null && cookie["UserId"] != null)
            {
                int userId = int.Parse(cookie["UserId"]);
                Session["UserId"] = userId;
                return userId;
            }
            return null;
        }

        public ActionResult Index()
        {
            // Kiểm tra đăng nhập
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            Cart cart = (Cart)Session["Cart"];
            if (cart == null)
            {
                // Load từ database khi Session rỗng
                int? maKH = GetMaKH();
                if (maKH.HasValue)
                {
                    cart = LoadCartFromDatabase(maKH.Value);
                    Session["Cart"] = cart;
                }
                else
                {
                    cart = new Cart();
                }
            }
            return View(cart);
        }

        public ActionResult AddCartItem(int id, int soLuong)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            int? maKH = GetMaKH();
            if (!maKH.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            if (soLuong <= 0)
            {
                soLuong = 1;
            }

            Cart cart = (Cart)Session["Cart"];

            if (cart == null)
            {
                // Load từ database nếu chưa có
                cart = LoadCartFromDatabase(maKH.Value);
            }

            int them = cart.ThemSP(id, soLuong);
            if (them == -1)
            {
                TempData["Error"] = "Sản phẩm không tồn tại!";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = id });
            }
            else if (them == -2)
            {
                // Lấy thông tin sản phẩm để báo số lượng còn lại rõ ràng
                var sp = data.tblSanPham.SingleOrDefault(x => x.MaSP == id);
                int available = sp?.SoLuongTon ?? 0;
                string ten = sp?.TenSP ?? ("#" + id);
                TempData["Error"] = $"Sản phẩm '{ten}' chỉ còn {available} trong kho. Vui lòng điều chỉnh số lượng (tối đa {available}).";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = id });
            }

            // thành công
            Session["Cart"] = cart;
            // Đồng bộ vào database
            SyncCartToDatabase(maKH.Value, cart);

            return RedirectToAction("ChiTietSanPham", "Home", new { id = id });
        }

        [HttpPost]
        public ActionResult MuaNgay(int id, int soLuong)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            int? maKH = GetMaKH();
            if (!maKH.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            if (soLuong <= 0)
            {
                soLuong = 1;
            }

            Cart cart = (Cart)Session["Cart"];
            if (cart == null)
            {
                // Load từ database nếu chưa có
                cart = LoadCartFromDatabase(maKH.Value);
            }

            int them = cart.ThemSP(id, soLuong);

            if (them == -1)
            {
                TempData["Error"] = "Sản phẩm không tồn tại!";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = id });
            }
            else if (them == -2)
            {
                var sp = data.tblSanPham.SingleOrDefault(x => x.MaSP == id);
                int available = sp?.SoLuongTon ?? 0;
                string ten = sp?.TenSP ?? ("#" + id);
                TempData["Error"] = $"Sản phẩm '{ten}' chỉ còn {available} trong kho. Vui lòng điều chỉnh số lượng (tối đa {available}).";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = id });
            }

            Session["Cart"] = cart;
            // Đồng bộ vào database
            SyncCartToDatabase(maKH.Value, cart);

            return RedirectToAction("Index", "Cart");
        }

        public ActionResult RemoveCartItem(int id)
        {
            int? maKH = GetMaKH();
            if (!maKH.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            Cart cart = (Cart)Session["Cart"];
            if (cart == null)
            {
                cart = LoadCartFromDatabase(maKH.Value);
            }

            int check = cart.XoaSP(id);
            if (check != 1)
            {
                TempData["Error"] = "Xóa không thành công!";
                return RedirectToAction("Index", "Cart");
            }

            Session["Cart"] = cart;
            // Đồng bộ vào database
            SyncCartToDatabase(maKH.Value, cart);

            return RedirectToAction("Index", "Cart");
        }

        public ActionResult UpdateQuanity(int id, int thaotac)
        {
            int? maKH = GetMaKH();
            if (!maKH.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            Cart cart = (Cart)Session["Cart"];
            if (cart == null)
            {
                cart = LoadCartFromDatabase(maKH.Value);
            }

            int check = cart.CapNhatSL(id, thaotac);
            if (check == -1)
            {
                TempData["Error"] = "Sản phẩm không tồn tại trong giỏ!";
                return RedirectToAction("Index", "Cart");
            }
            else if (check == -2)
            {
                // Lấy thông tin sản phẩm để báo rõ ràng
                var sp = data.tblSanPham.SingleOrDefault(x => x.MaSP == id);
                int available = sp?.SoLuongTon ?? 0;
                string ten = sp?.TenSP ?? ("#" + id);
                TempData["Error"] = $"Không thể tăng số lượng cho '{ten}' — chỉ còn {available} trong kho.";
                return RedirectToAction("Index", "Cart");
            }

            Session["Cart"] = cart;
            SyncCartToDatabase(maKH.Value, cart);

            return RedirectToAction("Index", "Cart");
        }

        public ActionResult PaymentConfirm()
        {
            HttpCookie cookie = Request.Cookies["UserLogin"];
            if (cookie != null)
            {
                Session["UserId"] = cookie["UserId"];
            }
            int ID_kh = int.Parse(Session["UserId"].ToString());
            tblKhachHang khachHang = data.tblKhachHang.FirstOrDefault(x => x.MaKH == ID_kh);
            Cart cart = (Cart)Session["Cart"];
            if (cart == null || cart.List_SP.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // Kiểm tra tồn kho trước khi tạo hóa đơn
            foreach (var item in cart.List_SP)
            {
                var sanPham = data.tblSanPham.SingleOrDefault(x => x.MaSP == item.MaSP);
                int available = sanPham?.SoLuongTon ?? 0;
                if (item.soLuong > available)
                {
                    TempData["Error"] = $"Sản phẩm '{sanPham?.TenSP ?? item.MaSP.ToString()}' chỉ còn {available} trong kho. Vui lòng điều chỉnh giỏ hàng.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            tblHoaDon hoaDon = new tblHoaDon
            {
                MaKH = khachHang.MaKH,
                MaNV = 1,
                NgayLap = DateTime.Now,
                TongTien = cart.TongTT(),
                TinhTrang = 1,
                DiaChiGiaoHang = khachHang.DiaChi,
                DaThanhToan = false
            };

            data.tblHoaDon.Add(hoaDon);
            data.SaveChanges();

            int maHoaDonMoi = hoaDon.MaHD;

            foreach (var item in cart.List_SP)
            {
                tblChiTietHoaDon chiTiet = new tblChiTietHoaDon
                {
                    MaHD = maHoaDonMoi,
                    MaSP = item.MaSP,
                    SoLuong = item.soLuong,
                    GiaBan = item.Gia
                };
                data.tblChiTietHoaDon.Add(chiTiet);

                var sanPham = data.tblSanPham.SingleOrDefault(x => x.MaSP == item.MaSP);
                if (sanPham != null)
                {
                    sanPham.SoLuongTon = sanPham.SoLuongTon - item.soLuong;
                }
            }

            data.SaveChanges();

            // Xóa giỏ hàng trong database sau khi thanh toán
            var gioHangItems = data.tblGioHang.Where(x => x.MaKH == ID_kh).ToList();
            foreach (var item in gioHangItems)
            {
                data.tblGioHang.Remove(item);
            }
            data.SaveChanges();

            cart = new Cart();
            Session["Cart"] = cart;
            return View();
        }

        // Phương thức load giỏ hàng từ database
        private Cart LoadCartFromDatabase(int maKH)
        {
            Cart cart = new Cart();
            var gioHangItems = data.tblGioHang.Where(x => x.MaKH == maKH).ToList();

            foreach (var item in gioHangItems)
            {
                if (item.MaSP.HasValue && item.SoLuong.HasValue)
                {
                    // Lấy tồn kho hiện tại và giới hạn số lượng khi load
                    var sp = data.tblSanPham.FirstOrDefault(s => s.MaSP == item.MaSP.Value);
                    int available = sp?.SoLuongTon ?? 0;
                    int qtyToAdd = Math.Min(item.SoLuong.Value, available);
                    if (qtyToAdd > 0)
                    {
                        cart.ThemSP(item.MaSP.Value, qtyToAdd);
                    }
                    // nếu qtyToAdd == 0: bỏ qua (hết hàng)
                }
            }

            return cart;
        }

        private void SyncCartToDatabase(int maKH, Cart cart)
        {
            // Xóa tất cả items cũ của khách hàng
            var oldItems = data.tblGioHang.Where(x => x.MaKH == maKH).ToList();
            foreach (var item in oldItems)
            {
                data.tblGioHang.Remove(item);
            }

            // Thêm items mới
            if (cart != null && cart.List_SP != null)
            {
                foreach (var item in cart.List_SP)
                {
                    tblGioHang gioHangItem = new tblGioHang
                    {
                        MaKH = maKH,
                        MaSP = item.MaSP,
                        SoLuong = item.soLuong,
                        NgayThem = DateTime.Now
                    };
                    data.tblGioHang.Add(gioHangItem);
                }
            }

            data.SaveChanges();
        }
    }
}