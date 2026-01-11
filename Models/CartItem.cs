using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ĐỒ_ÁN.Models
{
    public class CartItem
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public string HinhAnh { get; set; }
        public int soLuong { get; set; }
        public decimal Gia { get; set; }
        public decimal ThanhTien
        {
            get
            {
                return soLuong * Gia;
            }
        }

        QL_VanPhongPhamEntities data = new QL_VanPhongPhamEntities();
        public CartItem() { }
        public CartItem(int ma, int sl)
        {
            tblSanPham sp = new tblSanPham();
            sp = data.tblSanPham.FirstOrDefault(x => x.MaSP == ma);
            if (sp != null)
            {
                MaSP = sp.MaSP;
                TenSP = sp.TenSP;
                HinhAnh = sp.AnhDaiDien;
                soLuong = sl;
                Gia = (decimal)sp.GiaBan;
            }
        }
    }
}