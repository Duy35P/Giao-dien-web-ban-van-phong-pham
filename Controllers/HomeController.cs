using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
namespace ĐỒ_ÁN.Controllers
{
    public class HomeController : Controller
    {
        QL_VanPhongPhamEntities db = new QL_VanPhongPhamEntities();

        public ActionResult Index()
        {
            
            var sanPhamNoiBat = db.tblSanPham
                .Where(x => x.TrangThai == true)  
                .OrderByDescending(x => x.MaSP)
                .Take(5)
                .ToList();
            return View(sanPhamNoiBat);
        }

        public ActionResult SP()
        {
            // Chỉ hiển thị sản phẩm đang bán
            return View(db.tblSanPham.Where(x => x.TrangThai == true).ToList());
        }

        public ActionResult _LoaiSP()
        {
            return PartialView(db.tblLoaiSP.ToList());
        }

        public ActionResult _NCC()
        {
            return PartialView(db.tblNhaCungCap.ToList());
        }

        // API: Lấy danh sách Sản Phẩm
        [HttpGet]
        public JsonResult GetSanPham()
        {
            try
            {
                var danhSachSanPham = db.tblSanPham
                    .Where(x => x.TrangThai == true)
                    .Select(x => new
                    {
                        x.MaSP,
                        x.TenSP,
                        x.GiaBan,
                        x.SoLuongTon,
                        x.MoTaChiTiet,
                        x.AnhDaiDien,
                        x.LoaiSP,
                        TenLoai = x.tblLoaiSP.TenLoai,
                        x.NCC,
                        x.TrangThai
                    })
                    .ToList();

                return Json(new { success = true, data = danhSachSanPham }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult TimTheoLoai(int id)
        {
            // Lọc theo loại và chỉ lấy sản phẩm đang bán
            List<tblSanPham> list = db.tblSanPham
                .Where(x => x.LoaiSP == id && x.TrangThai == true)
                .ToList();
            return View("SP", list);
        }

        public ActionResult TimTheoNCC(int id)
        {
            // Lọc theo NCC và chỉ lấy sản phẩm đang bán
            List<tblSanPham> list = db.tblSanPham
                .Where(x => x.NCC == id && x.TrangThai == true)
                .ToList();
            return View("SP", list);
        }

        public ActionResult TimKiemTheoTuKhoa(string keyword)
        {
            // Tìm kiếm và chỉ lấy sản phẩm đang bán
            List<tblSanPham> list = db.tblSanPham
                .Where(x => x.TenSP.ToLower().Contains(keyword.ToLower()) && x.TrangThai == true)
                .ToList();
            return View("SP", list);
        }

        public ActionResult TimKiemNangCao(string keyword, int? loai, decimal? min, decimal? max, string sort)
        {
            List<tblSanPham> result = new List<tblSanPham>();

            bool isSearching = !string.IsNullOrEmpty(keyword) || loai.HasValue || min.HasValue || max.HasValue || !string.IsNullOrEmpty(sort);

            if (isSearching)
            {
                // Thêm điều kiện TrangThai = true
                var sp = db.tblSanPham.Where(x => x.TrangThai == true).AsQueryable();

                if (!string.IsNullOrEmpty(keyword))
                {
                    sp = sp.Where(s => s.TenSP.Contains(keyword));
                }

                if (loai.HasValue)
                {
                    sp = sp.Where(s => s.LoaiSP == loai.Value);
                }

                if (min.HasValue)
                {
                    sp = sp.Where(s => s.GiaBan >= min.Value);
                }

                if (max.HasValue)
                {
                    sp = sp.Where(s => s.GiaBan <= max.Value);
                }

                if (!string.IsNullOrEmpty(sort))
                {
                    switch (sort)
                    {
                        case "price_asc":
                            sp = sp.OrderBy(s => s.GiaBan);
                            break;
                        case "price_desc":
                            sp = sp.OrderByDescending(s => s.GiaBan);
                            break;
                    }
                }

                result = sp.ToList();
                ViewBag.HasSearched = true;
                ViewBag.resultCount = result.Count;
            }
            else
            {
                ViewBag.HasSearched = false;
            }

            ViewBag.keyword = keyword;
            ViewBag.loai = loai;
            ViewBag.min = min;
            ViewBag.max = max;
            ViewBag.sort = sort;
            ViewBag.DanhSachLoai = db.tblLoaiSP.ToList();

            return View(result);
        }

        public ActionResult ChiTietSanPham(int id)
        {
            var sanPham = db.tblSanPham.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Không cho xem chi tiết sản phẩm đã tắt
            if (!sanPham.TrangThai)
            {
                TempData["Error"] = "Sản phẩm này hiện không còn bán.";
                return RedirectToAction("SP");
            }

            var allReviews = db.tblDanhGia
                               .Where(d => d.MaSP == id)
                               .ToList();

            var validReviews = allReviews
                                 .Where(d => d.DiemDanhGia.HasValue)
                                 .ToList();

            // Tính toán tổng quan
            int totalReviews = validReviews.Count();
            double avgRating = 0;

            if (totalReviews > 0)
            {
                avgRating = validReviews.Average(d => d.DiemDanhGia.Value);
            }

            // Đếm số sao
            ViewBag.Dem5Sao = validReviews.Count(d => d.DiemDanhGia == 5);
            ViewBag.Dem4Sao = validReviews.Count(d => d.DiemDanhGia == 4);
            ViewBag.Dem3Sao = validReviews.Count(d => d.DiemDanhGia == 3);
            ViewBag.Dem2Sao = validReviews.Count(d => d.DiemDanhGia == 2);
            ViewBag.Dem1Sao = validReviews.Count(d => d.DiemDanhGia == 1);

            // Gán các giá trị vào ViewBag
            ViewBag.TotalReviews = totalReviews;
            ViewBag.AvgRating = avgRating;
            ViewBag.DanhGia = allReviews;

            // Chỉ lấy sản phẩm liên quan đang bán
            ViewBag.SanPhamLienQuan = db.tblSanPham
                                        .Where(s => s.LoaiSP == sanPham.LoaiSP && s.MaSP != id && s.TrangThai == true)
                                        .Take(10).ToList();
            ViewBag.CacAnhPhu = db.tblHinhAnh.Where(h => h.MaSP == id).ToList();

            return View(sanPham);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemDanhGia(int MaSP, int DiemDanhGia, string BinhLuan)
        {
            if (Session["UserName"] == null || Session["UserId"] == null)
            {
                TempData["ReviewError"] = "Bạn cần đăng nhập để đánh giá.";
                return RedirectToAction("Login", "User");
            }

            int maKH = 0;
            try
            {
                maKH = int.Parse(Session["UserId"].ToString());
            }
            catch
            {
                TempData["ReviewError"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = MaSP });
            }

            if (DiemDanhGia < 1 || DiemDanhGia > 5)
            {
                TempData["ReviewError"] = "Vui lòng chọn số sao đánh giá.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = MaSP });
            }

            try
            {
                tblDanhGia newReview = new tblDanhGia
                {
                    MaSP = MaSP,
                    MaKH = maKH,
                    DiemDanhGia = DiemDanhGia,
                    BinhLuan = BinhLuan,
                    NgayDanhGia = DateTime.Now
                };

                db.tblDanhGia.Add(newReview);
                db.SaveChanges();

                TempData["ReviewSuccess"] = "Gửi đánh giá thành công!";
            }
            catch (Exception ex)
            {
                TempData["ReviewError"] = "Đã xảy ra lỗi khi gửi đánh giá.";
            }

            return RedirectToAction("ChiTietSanPham", "Home", new { id = MaSP });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaDanhGia(int maDG, int maSP)
        {
            if (Session["UserId"] == null)
            {
                TempData["ReviewError"] = "Bạn cần đăng nhập để thực hiện việc này.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
            }

            int maKH_Session = 0;
            try
            {
                maKH_Session = int.Parse(Session["UserId"].ToString());
            }
            catch
            {
                TempData["ReviewError"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
            }

            var danhGia = db.tblDanhGia.Find(maDG);

            if (danhGia == null)
            {
                TempData["ReviewError"] = "Không tìm thấy đánh giá để xóa.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
            }

            if (danhGia.MaKH != maKH_Session)
            {
                TempData["ReviewError"] = "Bạn không có quyền xóa đánh giá của người khác.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
            }

            try
            {
                db.tblDanhGia.Remove(danhGia);
                db.SaveChanges();
                TempData["ReviewSuccess"] = "Đã xóa đánh giá thành công.";
            }
            catch (Exception ex)
            {
                TempData["ReviewError"] = "Đã xảy ra lỗi khi xóa đánh giá.";
            }

            return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaDanhGia(int maDG, int maSP, int DiemDanhGia, string BinhLuan)
        {
            if (Session["UserId"] == null)
            {
                TempData["ReviewError"] = "Bạn cần đăng nhập để thực hiện việc này.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
            }

            int maKH_Session = 0;
            try
            {
                maKH_Session = int.Parse(Session["UserId"].ToString());
            }
            catch
            {
                TempData["ReviewError"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
            }

            var danhGia = db.tblDanhGia.Find(maDG);

            if (danhGia == null)
            {
                TempData["ReviewError"] = "Không tìm thấy đánh giá.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
            }

            if (danhGia.MaKH != maKH_Session)
            {
                TempData["ReviewError"] = "Bạn không có quyền sửa đánh giá của người khác.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
            }

            if (DiemDanhGia < 1 || DiemDanhGia > 5)
            {
                TempData["ReviewError"] = "Vui lòng chọn số sao đánh giá.";
                return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
            }

            try
            {
                danhGia.DiemDanhGia = DiemDanhGia;
                danhGia.BinhLuan = BinhLuan;
                danhGia.NgayDanhGia = DateTime.Now;

                db.Entry(danhGia).State = System.Data.EntityState.Modified;
                db.SaveChanges();
                TempData["ReviewSuccess"] = "Cập nhật đánh giá thành công.";
            }
            catch (Exception ex)
            {
                TempData["ReviewError"] = "Đã xảy ra lỗi khi cập nhật đánh giá.";
            }

            return RedirectToAction("ChiTietSanPham", "Home", new { id = maSP });
        }
    }
}