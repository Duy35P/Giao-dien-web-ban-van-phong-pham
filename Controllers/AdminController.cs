using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;  

namespace ĐỒ_ÁN.Controllers
{
    public class AdminController : Controller
    {
        QL_VanPhongPhamEntities db = new QL_VanPhongPhamEntities();

      
        public ActionResult Login()
        {
            if (Session["NVId"] != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

     
        [HttpPost]
        public ActionResult LoginSubmit(string TenNV, string Password)
        {
            tblNhanVien nv = db.tblNhanVien.FirstOrDefault(x => x.TenNV == TenNV && x.MatKhau == Password);

            if (nv == null)
            {
                Session["Error"] = "Thông tin đăng nhập không đúng!";
                return RedirectToAction("Login", "Admin");
            }

            Session["Error"] = null;
            Session["NVName"] = nv.TenNV;
            Session["NVId"] = nv.MaNV;
            Session["VaiTro"] = nv.VaiTro;

            HttpCookie cookie = new HttpCookie("NVLogin");
            cookie["NVId"] = nv.MaNV.ToString();
            cookie["NVName"] = nv.TenNV;
            cookie["VaiTro"] = nv.VaiTro.ToString();
            cookie.Expires = DateTime.Now.AddDays(3);
            Response.Cookies.Add(cookie);

            return RedirectToAction("Index", "Admin");
        }

     
        public ActionResult Index()
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            ViewBag.TongSanPham = db.tblSanPham.Count();
            ViewBag.TongKhachHang = db.tblKhachHang.Count();
            ViewBag.TongDonHang = db.tblHoaDon.Count();
            ViewBag.TongDoanhThu = db.tblHoaDon.Where(x => x.DaThanhToan == true).Sum(x => (decimal?)x.TongTien) ?? 0;

            return View();
        }

       
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            if (Request.Cookies["NVLogin"] != null)
            {
                HttpCookie cookie = new HttpCookie("NVLogin");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Login", "Admin");
        }

        //  QUẢN LÝ SẢN PHẨM   
        public ActionResult QuanLySanPham()
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var sanPhams = db.tblSanPham
                .Include(s => s.tblLoaiSP)
                .Include(s => s.tblNhaCungCap)
                .OrderByDescending(s => s.MaSP)
                .ToList();

            // Thêm danh sách loại sản phẩm cho filter
            ViewBag.DanhSachLoai = db.tblLoaiSP.ToList();

            return View(sanPhams);
        }

      
        public ActionResult ThemSanPham()
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            ViewBag.LoaiSP = new SelectList(db.tblLoaiSP, "MaLoai", "TenLoai");
            ViewBag.NCC = new SelectList(db.tblNhaCungCap, "MaNCC", "TenNCC");
            return View();
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSanPham(tblSanPham sp, HttpPostedFileBase[] AnhSanPhamFiles)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload nhiều ảnh
                    if (AnhSanPhamFiles != null && AnhSanPhamFiles.Length > 0)
                    {
                        bool isFirstImage = true;

                        foreach (var file in AnhSanPhamFiles)
                        {
                            if (file != null && file.ContentLength > 0)
                            {
                                string fileName = Path.GetFileName(file.FileName);
                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                                string path = Path.Combine(Server.MapPath("~/Content/Images"), uniqueFileName);
                                file.SaveAs(path);

                                // Ảnh đầu tiên lưu vào AnhDaiDien
                                if (isFirstImage)
                                {
                                    sp.AnhDaiDien = uniqueFileName;
                                    isFirstImage = false;
                                }
                                else
                                {
                                    // Các ảnh còn lại lưu vào tblHinhAnh (phải sau khi có MaSP)
                                    // Tạm lưu vào list để xử lý sau
                                }
                            }
                        }
                    }
                    else
                    {
                        sp.AnhDaiDien = "no-image.jpg";
                    }

                    // Lưu sản phẩm để có MaSP
                    db.tblSanPham.Add(sp);
                    db.SaveChanges();

                    // Bây giờ lưu các ảnh còn lại vào tblHinhAnh
                    if (AnhSanPhamFiles != null && AnhSanPhamFiles.Length > 1)
                    {
                        bool skipFirst = true;

                        foreach (var file in AnhSanPhamFiles)
                        {
                            if (file != null && file.ContentLength > 0)
                            {
                                // Bỏ qua ảnh đầu tiên (đã lưu vào AnhDaiDien)
                                if (skipFirst)
                                {
                                    skipFirst = false;
                                    continue;
                                }

                                string fileName = Path.GetFileName(file.FileName);
                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                                string path = Path.Combine(Server.MapPath("~/Content/Images"), uniqueFileName);
                                file.SaveAs(path);

                                tblHinhAnh hinhAnh = new tblHinhAnh
                                {
                                    MaSP = sp.MaSP,
                                    TenHinh = uniqueFileName
                                };
                                db.tblHinhAnh.Add(hinhAnh);
                            }
                        }
                        db.SaveChanges();
                    }

                    TempData["Success"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction("QuanLySanPham");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi: " + ex.Message;
                }
            }

            ViewBag.LoaiSP = new SelectList(db.tblLoaiSP, "MaLoai", "TenLoai", sp.LoaiSP);
            ViewBag.NCC = new SelectList(db.tblNhaCungCap, "MaNCC", "TenNCC", sp.NCC);
            return View(sp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaSanPham(tblSanPham sp, HttpPostedFileBase[] AnhSanPhamFiles)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var oldSP = db.tblSanPham.AsNoTracking().FirstOrDefault(x => x.MaSP == sp.MaSP);

                    // Xử lý upload ảnh mới
                    if (AnhSanPhamFiles != null && AnhSanPhamFiles.Length > 0 && AnhSanPhamFiles[0] != null)
                    {
                        bool isFirstImage = true;

                        foreach (var file in AnhSanPhamFiles)
                        {
                            if (file != null && file.ContentLength > 0)
                            {
                                string fileName = Path.GetFileName(file.FileName);
                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                                string path = Path.Combine(Server.MapPath("~/Content/Images"), uniqueFileName);
                                file.SaveAs(path);

                                // Ảnh đầu tiên thay thế AnhDaiDien
                                if (isFirstImage)
                                {
                                    // Xóa ảnh đại diện cũ
                                    if (!string.IsNullOrEmpty(oldSP.AnhDaiDien) && oldSP.AnhDaiDien != "no-image.jpg")
                                    {
                                        string oldPath = Path.Combine(Server.MapPath("~/Content/Images"), oldSP.AnhDaiDien);
                                        if (System.IO.File.Exists(oldPath))
                                        {
                                            System.IO.File.Delete(oldPath);
                                        }
                                    }

                                    sp.AnhDaiDien = uniqueFileName;
                                    isFirstImage = false;
                                }
                                else
                                {
                                    // Các ảnh còn lại thêm vào tblHinhAnh
                                    tblHinhAnh hinhAnh = new tblHinhAnh
                                    {
                                        MaSP = sp.MaSP,
                                        TenHinh = uniqueFileName
                                    };
                                    db.tblHinhAnh.Add(hinhAnh);
                                }
                            }
                        }
                    }
                    else
                    {
                        sp.AnhDaiDien = oldSP.AnhDaiDien; // Giữ ảnh cũ
                    }

                    db.Entry(sp).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("QuanLySanPham");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi: " + ex.Message;
                }
            }

            ViewBag.LoaiSP = new SelectList(db.tblLoaiSP, "MaLoai", "TenLoai", sp.LoaiSP);
            ViewBag.NCC = new SelectList(db.tblNhaCungCap, "MaNCC", "TenNCC", sp.NCC);
            return View(sp);
        }

        public ActionResult SuaSanPham(int id)
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var sp = db.tblSanPham.Find(id);
            if (sp == null)
            {
                return HttpNotFound();
            }

            // Load ảnh mô tả từ bảng tblHinhAnh
            var hinhAnhs = db.tblHinhAnh.Where(h => h.MaSP == id).ToList();
            ViewBag.HinhAnhs = hinhAnhs;

            ViewBag.LoaiSP = new SelectList(db.tblLoaiSP, "MaLoai", "TenLoai", sp.LoaiSP);
            ViewBag.NCC = new SelectList(db.tblNhaCungCap, "MaNCC", "TenNCC", sp.NCC);
            return View(sp);
        }

        [HttpPost]
        public JsonResult XoaHinhAnh(int id)
        {
            try
            {
                var hinhAnh = db.tblHinhAnh.Find(id);
                if (hinhAnh != null)
                {
                    // Xóa file vật lý
                    string path = Path.Combine(Server.MapPath("~/Content/Images"), hinhAnh.TenHinh);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    // Xóa trong database
                    db.tblHinhAnh.Remove(hinhAnh);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Xóa ảnh thành công!" });
                }
                return Json(new { success = false, message = "Không tìm thấy ảnh!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ToggleTrangThaiSanPham(int id)
        {
            try
            {
                var sp = db.tblSanPham.Find(id);
                if (sp != null)
                {
                    // Đảo ngược trạng thái
                    sp.TrangThai = !sp.TrangThai;
                    db.SaveChanges();
                    
                    string trangThaiMoi = sp.TrangThai ? "Đang bán" : "Tắt";
                    TempData["Success"] = $"Đã chuyển sản phẩm '{sp.TenSP}' sang trạng thái: {trangThaiMoi}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("QuanLySanPham");
        }

        // QUẢN LÝ ĐƠN HÀNG 
        public ActionResult QuanLyDonHang()
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var donHangs = db.tblHoaDon
                .Include(d => d.tblKhachHang)
                .Include(d => d.tblNhanVien)
                .Include(d => d.tblTinhTrang)
                .OrderByDescending(d => d.NgayLap)
                .ToList();

            return View(donHangs);
        }

        public ActionResult ChiTietDonHang(int id)
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var donHang = db.tblHoaDon
                .Include(d => d.tblKhachHang)
                .Include(d => d.tblNhanVien)
                .Include(d => d.tblTinhTrang)
                .FirstOrDefault(d => d.MaHD == id);

            if (donHang == null)
            {
                return HttpNotFound();
            }

            var chiTiet = db.tblChiTietHoaDon
                .Include(c => c.tblSanPham)
                .Where(c => c.MaHD == id)
                .ToList();

            ViewBag.ChiTiet = chiTiet;
            ViewBag.TinhTrangs = new SelectList(db.tblTinhTrang, "ID", "TinhTrangHoaDon", donHang.TinhTrang);
            return View(donHang);
        }

        [HttpPost]
        public ActionResult CapNhatTinhTrang(int MaHD, int TinhTrang)
        {
            var donHang = db.tblHoaDon.Find(MaHD);
            if (donHang != null)
            {
                donHang.TinhTrang = TinhTrang;
                donHang.MaNV = (int)Session["NVId"];
                db.SaveChanges();
                TempData["Success"] = "Cập nhật tình trạng đơn hàng thành công!";
            }
            return RedirectToAction("ChiTietDonHang", new { id = MaHD });
        }


        [HttpPost]
        public ActionResult CapNhatThanhToan(int MaHD, bool DaThanhToan)
        {
            var donHang = db.tblHoaDon.Find(MaHD);
            if (donHang != null)
            {
                donHang.DaThanhToan = DaThanhToan;
                donHang.MaNV = (int)Session["NVId"];
                db.SaveChanges();
                TempData["Success"] = "Cập nhật trạng thái thanh toán thành công!";
            }
            return RedirectToAction("ChiTietDonHang", new { id = MaHD });
        }

        // Cập nhật cả tình trạng và thanh toán
        [HttpPost]
        public ActionResult CapNhatDonHang(int MaHD, int TinhTrang, bool DaThanhToan)
        {
            var donHang = db.tblHoaDon.Find(MaHD);
            if (donHang != null)
            {
                donHang.TinhTrang = TinhTrang;
                donHang.DaThanhToan = DaThanhToan;
                donHang.MaNV = (int)Session["NVId"];
                db.SaveChanges();
                TempData["Success"] = "Cập nhật đơn hàng thành công!";
            }
            return RedirectToAction("ChiTietDonHang", new { id = MaHD });
        }

        //QUẢN LÝ KHÁCH HÀNG 

        public ActionResult QuanLyKhachHang()
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var khachHangs = db.tblKhachHang.OrderByDescending(k => k.MaKH).ToList();
            return View(khachHangs);
        }

        public ActionResult ChiTietKhachHang(int id)
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var khachHang = db.tblKhachHang.Find(id);
            if (khachHang == null)
            {
                return HttpNotFound();
            }

            // Lấy lịch sử mua hàng
            var lichSuMuaHang = db.tblHoaDon
                .Include(h => h.tblTinhTrang)
                .Where(h => h.MaKH == id)
                .OrderByDescending(h => h.NgayLap)
                .ToList();

            ViewBag.LichSuMuaHang = lichSuMuaHang;
            ViewBag.TongDonHang = lichSuMuaHang.Count;
            ViewBag.TongChiTieu = lichSuMuaHang.Where(h => h.DaThanhToan == true).Sum(h => (decimal?)h.TongTien) ?? 0;

            return View(khachHang);
        }

        [HttpPost]
        public ActionResult XoaKhachHang(int id)
        {
            try
            {
                var kh = db.tblKhachHang.Find(id);
                if (kh != null)
                {
                    // Kiểm tra xem khách hàng có đơn hàng không
                    var coDonHang = db.tblHoaDon.Any(h => h.MaKH == id);
                    if (coDonHang)
                    {
                        TempData["Error"] = "Không thể xóa khách hàng đã có đơn hàng!";
                    }
                    else
                    {
                        // Xóa giỏ hàng của khách hàng
                        var gioHangs = db.tblGioHang.Where(g => g.MaKH == id).ToList();
                        foreach (var gh in gioHangs)
                        {
                            db.tblGioHang.Remove(gh);
                        }

                        // Xóa đánh giá của khách hàng
                        var danhGias = db.tblDanhGia.Where(d => d.MaKH == id).ToList();
                        foreach (var dg in danhGias)
                        {
                            db.tblDanhGia.Remove(dg);
                        }

                        db.tblKhachHang.Remove(kh);
                        db.SaveChanges();
                        TempData["Success"] = "Xóa khách hàng thành công!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("QuanLyKhachHang");
        }

        // QUẢN LÝ NHÂN VIÊN 
        public ActionResult QuanLyNhanVien()
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            // Chỉ Admin mới được quản lý nhân viên
            if (Session["VaiTro"] != null && (int)Session["VaiTro"] != 1)
            {
                TempData["Error"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Index", "Admin");
            }

            var nhanViens = db.tblNhanVien
                .Include(n => n.tblVaiTro)
                .OrderByDescending(n => n.MaNV)
                .ToList();

            return View(nhanViens);
        }

        public ActionResult ThemNhanVien()
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            // Chỉ Admin mới được thêm nhân viên
            if (Session["VaiTro"] != null && (int)Session["VaiTro"] != 1)
            {
                TempData["Error"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Index", "Admin");
            }

            ViewBag.VaiTro = new SelectList(db.tblVaiTro, "IDVaiTro", "TenVaiTro");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemNhanVien(tblNhanVien nv)
        {
            if (Session["VaiTro"] != null && (int)Session["VaiTro"] != 1)
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index", "Admin");
            }

            // Kiểm tra tên nhân viên đã tồn tại chưa
            var nhanVienTonTai = db.tblNhanVien.FirstOrDefault(x => x.TenNV == nv.TenNV);
            if (nhanVienTonTai != null)
            {
                ModelState.AddModelError("TenNV", "Tên đăng nhập đã tồn tại!");
                ViewBag.VaiTro = new SelectList(db.tblVaiTro, "IDVaiTro", "TenVaiTro", nv.VaiTro);
                return View(nv);
            }

            if (ModelState.IsValid)
            {
                db.tblNhanVien.Add(nv);
                db.SaveChanges();
                TempData["Success"] = "Thêm nhân viên thành công!";
                return RedirectToAction("QuanLyNhanVien");
            }

            ViewBag.VaiTro = new SelectList(db.tblVaiTro, "IDVaiTro", "TenVaiTro", nv.VaiTro);
            return View(nv);
        }

        public ActionResult SuaNhanVien(int id)
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            if (Session["VaiTro"] != null && (int)Session["VaiTro"] != 1)
            {
                TempData["Error"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Index", "Admin");
            }

            var nv = db.tblNhanVien.Find(id);
            if (nv == null)
            {
                return HttpNotFound();
            }

            ViewBag.VaiTro = new SelectList(db.tblVaiTro, "IDVaiTro", "TenVaiTro", nv.VaiTro);
            return View(nv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaNhanVien(tblNhanVien nv)
        {
            if (Session["VaiTro"] != null && (int)Session["VaiTro"] != 1)
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index", "Admin");
            }

            // Kiểm tra tên nhân viên trùng (trừ chính nó)
            var nhanVienTrung = db.tblNhanVien.FirstOrDefault(x => x.TenNV == nv.TenNV && x.MaNV != nv.MaNV);
            if (nhanVienTrung != null)
            {
                ModelState.AddModelError("TenNV", "Tên đăng nhập đã tồn tại!");
                ViewBag.VaiTro = new SelectList(db.tblVaiTro, "IDVaiTro", "TenVaiTro", nv.VaiTro);
                return View(nv);
            }

            if (ModelState.IsValid)
            {
                db.Entry(nv).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Success"] = "Cập nhật nhân viên thành công!";
                return RedirectToAction("QuanLyNhanVien");
            }

            ViewBag.VaiTro = new SelectList(db.tblVaiTro, "IDVaiTro", "TenVaiTro", nv.VaiTro);
            return View(nv);
        }

        [HttpPost]
        public ActionResult XoaNhanVien(int id)
        {
            if (Session["VaiTro"] != null && (int)Session["VaiTro"] != 1)
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("QuanLyNhanVien");
            }

            try
            {
                var nv = db.tblNhanVien.Find(id);
                if (nv != null)
                {
                    // Không cho phép xóa chính mình
                    if (nv.MaNV == (int)Session["NVId"])
                    {
                        TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập!";
                        return RedirectToAction("QuanLyNhanVien");
                    }

                    // Kiểm tra nhân viên có xử lý đơn hàng chưa
                    var coDonHang = db.tblHoaDon.Any(h => h.MaNV == id);
                    if (coDonHang)
                    {
                        TempData["Error"] = "Không thể xóa nhân viên đã xử lý đơn hàng!";
                    }
                    else
                    {
                        db.tblNhanVien.Remove(nv);
                        db.SaveChanges();
                        TempData["Success"] = "Xóa nhân viên thành công!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("QuanLyNhanVien");
        }

        public ActionResult ChiTietNhanVien(int id)
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var nhanVien = db.tblNhanVien
                .Include(n => n.tblVaiTro)
                .FirstOrDefault(n => n.MaNV == id);

            if (nhanVien == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách đơn hàng đã xử lý
            var donHangXuLy = db.tblHoaDon
                .Include(h => h.tblKhachHang)
                .Include(h => h.tblTinhTrang)
                .Where(h => h.MaNV == id)
                .OrderByDescending(h => h.NgayLap)
                .ToList();

            ViewBag.DonHangXuLy = donHangXuLy;
            ViewBag.TongDonHang = donHangXuLy.Count;
            ViewBag.TongDoanhThu = donHangXuLy.Where(h => h.DaThanhToan == true).Sum(h => (decimal?)h.TongTien) ?? 0;

            return View(nhanVien);
        }

        //BÁO CÁO DOANH THU

        public ActionResult BaoCaoDoanhThu(int? thang, int? nam)
        {
            if (Session["NVId"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            // Mặc định lấy tháng và năm hiện tại
            int selectedThang = thang ?? DateTime.Now.Month;
            int selectedNam = nam ?? DateTime.Now.Year;

            ViewBag.SelectedThang = selectedThang;
            ViewBag.SelectedNam = selectedNam;

            // Lấy danh sách đơn hàng đã thanh toán trong tháng
            var donHangs = db.tblHoaDon
                .Include(h => h.tblKhachHang)
                .Include(h => h.tblNhanVien)
                .Include(h => h.tblTinhTrang)
                .Where(h => h.NgayLap.Value.Month == selectedThang 
                         && h.NgayLap.Value.Year == selectedNam
                         && h.DaThanhToan == true)
                .OrderByDescending(h => h.NgayLap)
                .ToList();

            // Tính toán thống kê
            ViewBag.TongDonHang = donHangs.Count;
            ViewBag.TongDoanhThu = donHangs.Sum(h => (decimal?)h.TongTien) ?? 0;
            ViewBag.TrungBinhDonHang = donHangs.Count > 0 ? ViewBag.TongDoanhThu / donHangs.Count : 0;

            // Tổng đơn hàng chưa thanh toán trong tháng
            ViewBag.DonChuaThanhToan = db.tblHoaDon
                .Where(h => h.NgayLap.Value.Month == selectedThang 
                         && h.NgayLap.Value.Year == selectedNam
                         && h.DaThanhToan == false)
                .Count();

            // Danh sách năm có dữ liệu
            var danhSachNam = db.tblHoaDon
                .Where(h => h.NgayLap != null)
                .Select(h => h.NgayLap.Value.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
            
            ViewBag.DanhSachNam = danhSachNam;

            return View(donHangs);
        }
    }
}