using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ĐỒ_ÁN.Controllers
{
    public class UserController : Controller
    {
        QL_VanPhongPhamEntities db = new QL_VanPhongPhamEntities();
        // GET: User
        public ActionResult Login()
        {
            return View();
        }
        public ActionResult Regist()
        {
            return View();
        }

        public ActionResult LoginSubmit(string Email, string Password)
        {
            tblKhachHang kh = new tblKhachHang();
            kh = db.tblKhachHang.FirstOrDefault(x => x.Email == Email && x.MatKhau == Password);
            if (kh == null)
            {
                Session["Error"] = "Thông tin đăng nhập không đúng!";
                return RedirectToAction("Login", "User");
            }
            Session["UserName"] = kh.TenKH;
            Session["UserId"] = kh.MaKH;

            // Lưu vào COOKIE (3 ngày)
            HttpCookie cookie = new HttpCookie("UserLogin");
            cookie["UserId"] = kh.MaKH.ToString();
            cookie["UserName"] = kh.TenKH;

            // Thời gian hết hạn
            cookie.Expires = DateTime.Now.AddDays(3);

            // Lưu xuống trình duyệt
            Response.Cookies.Add(cookie);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Logout()
        {
            // Xóa SESSION
            Session.Clear();
            Session.Abandon();

            // Xóa COOKIE lưu đăng nhập
            if (Request.Cookies["UserLogin"] != null)
            {
                HttpCookie cookie = new HttpCookie("UserLogin");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Login", "User");
        }

        public ActionResult SubmitRegist(string TenKH, string Email, int GioiTinh, string DienThoai, string DiaChi, string Password, string Re_Password)
        {
            if (Password != Re_Password)
            {
                Session["Error"] = "Mật khẩu nhập lại không khớp!";
                return RedirectToAction("Regist", "User");
            }
            if (db.tblKhachHang.FirstOrDefault(x => x.Email == Email) != null)
            {
                Session["Error"] = "Email đã được đăng kí!";
                return RedirectToAction("Regist", "User");
            }
            tblKhachHang kh = new tblKhachHang();
            kh.Email = Email;
            kh.TenKH = TenKH;
            if (GioiTinh == 1)
                kh.GioiTinh = "Nam";
            else
                kh.GioiTinh = "Nữ";
            kh.DienThoai = DienThoai;
            kh.DiaChi = DiaChi;
            kh.MatKhau = Password;
            db.tblKhachHang.Add(kh);
            db.SaveChanges();
            Session["UserName"] = kh.TenKH;
            Session["UserId"] = kh.MaKH;

            // Lưu vào COOKIE (3 ngày)
            HttpCookie cookie = new HttpCookie("UserLogin");
            cookie["UserId"] = kh.MaKH.ToString();
            cookie["UserName"] = kh.TenKH;

            // Thời gian hết hạn
            cookie.Expires = DateTime.Now.AddDays(3);

            // Lưu xuống trình duyệt
            Response.Cookies.Add(cookie);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult _Account_menu()
        {
            int id = Convert.ToInt32(Session["UserId"]);
            string avatar = db.tblKhachHang.FirstOrDefault(x => x.MaKH == id).Avarta;
            ViewBag.Avatar = avatar;
            return PartialView("_Account_menu");
        }

        public ActionResult User_Info()
        {
            int id = Convert.ToInt32(Session["UserId"]);
            tblKhachHang user = db.tblKhachHang.FirstOrDefault(x => x.MaKH == id);
            if (user == null)
            {
                return View("Index", "Home");
            }
            return View(user);
        }

        public ActionResult Update_info()
        {
            int id = Convert.ToInt32(Session["UserId"]);
            tblKhachHang user = db.tblKhachHang.FirstOrDefault(x => x.MaKH == id);
            return View(user);
        }

        public ActionResult Update_info_submit(tblKhachHang info)
        {
            if (ModelState.IsValid)
            {
                tblKhachHang info_update = db.tblKhachHang.Find(info.MaKH);
                info_update.TenKH = info.TenKH;
                info_update.MaKH = info.MaKH;
                info_update.GioiTinh = info.GioiTinh;
                info_update.Email = info.Email;
                info_update.DienThoai = info.DienThoai;
                info_update.NamSinh = info.NamSinh;
                info_update.DiaChi = info.DiaChi;

                // Update cả Session và Cookie
                Session["UserName"] = info_update.TenKH;
                
                // Cập nhật Cookie với tên mới
                if (Request.Cookies["UserLogin"] != null)
                {
                    HttpCookie cookie = new HttpCookie("UserLogin");
                    cookie["UserId"] = info_update.MaKH.ToString();
                    cookie["UserName"] = info_update.TenKH; // ← Cập nhật tên mới
                    cookie.Expires = DateTime.Now.AddDays(3);
                    Response.Cookies.Add(cookie);
                }

                db.SaveChanges();
                return RedirectToAction("User_Info");
            }
            else
            {
                ViewBag.Error_Update = "Lỗi không thể update!";
                return RedirectToAction("Update_Info");
            }

        }

        public ActionResult Purchase_Order()
        {
            int id = Convert.ToInt32(Session["UserId"]);
            List<tblHoaDon> list_order = db.tblHoaDon.Include("tblChiTietHoaDon").Include("tblChiTietHoaDon.tblSanPham").Where(x => x.MaKH == id).ToList();
            ViewBag.status = "all";
            return View(list_order);
        }

        public ActionResult Order_Pending()
        {
            int id = Convert.ToInt32(Session["UserId"]);
            List<tblHoaDon> list_order = db.tblHoaDon.Include("tblChiTietHoaDon").Include("tblChiTietHoaDon.tblSanPham").Where(x => x.MaKH == id && x.TinhTrang == 1).ToList();
            ViewBag.status = "pending";
            return View("Purchase_Order", list_order);
        }


        public ActionResult Order_Delivering()
        {
            int id = Convert.ToInt32(Session["UserId"]);
            List<tblHoaDon> list_order = db.tblHoaDon.Include("tblChiTietHoaDon").Include("tblChiTietHoaDon.tblSanPham").Where(x => x.MaKH == id && x.TinhTrang == 2).ToList();
            ViewBag.status = "delivering";
            return View("Purchase_Order", list_order);
        }

        public ActionResult Order_Done()
        {
            int id = Convert.ToInt32(Session["UserId"]);
            List<tblHoaDon> list_order = db.tblHoaDon.Include("tblChiTietHoaDon").Include("tblChiTietHoaDon.tblSanPham").Where(x => x.MaKH == id && x.TinhTrang == 3).ToList();
            ViewBag.status = "done";
            return View("Purchase_Order", list_order);
        }

        public ActionResult Order_Cancelled()
        {
            int id = Convert.ToInt32(Session["UserId"]);
            List<tblHoaDon> list_order = db.tblHoaDon.Include("tblChiTietHoaDon").Include("tblChiTietHoaDon.tblSanPham").Where(x => x.MaKH == id && x.TinhTrang == 4).ToList();
            ViewBag.status = "cancelled";
            return View("Purchase_Order", list_order);
        }

        [HttpPost]
        public ActionResult Update_Avatar(HttpPostedFileBase avatarFile)
        {
            if (avatarFile != null && avatarFile.ContentLength > 0)
            {
                string fileName = Path.GetFileName(avatarFile.FileName);
                string path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                avatarFile.SaveAs(path);

                int userId = Convert.ToInt32(Session["UserId"]);
                var user = db.tblKhachHang.Find(userId);
                user.Avarta = fileName;
                db.SaveChanges();
            }

            return RedirectToAction("User_Info");
        }

        public ActionResult Changepass()
        {
            return View();
        }

        public ActionResult Changepass_submit(string oldpass, string newpass)
        {
            if (oldpass != null && newpass != null)
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                tblKhachHang kh = db.tblKhachHang.Find(userId);
                if (kh != null)
                {
                    if (kh.MatKhau == oldpass)
                    {
                        kh.MatKhau = newpass;
                        db.SaveChanges();
                        ViewBag.Thanhcong = "Thay đổi mật khẩu thành công!";
                    }
                    else
                    {
                        ViewBag.Loi = "Sai mật khẩu hiện tại! Vui lòng nhập lại!";
                    }

                }
                else
                {
                    ViewBag.Loi = "Không tìm thấy thông tin tài khoản! Vui lòng đăng nhập lại";
                    return RedirectToAction("Login", "User");
                }
            }

            return View("Changepass");
        }


    }
}