using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ĐỒ_ÁN.Models
{
    public class Cart
    {
        QL_VanPhongPhamEntities data = new QL_VanPhongPhamEntities();
        public List<CartItem> List_SP = new List<CartItem>();
        //Phương thức cho giỏ hàng
        ///Đếm số sản phẩm khác nhau
        public int SoSP()
        {
            return List_SP.Count;
        }
        ///Tính tổng số lượng sản phẩm trong giỏ hàng
        public int TongSL()
        {
            return List_SP.Sum(x => x.soLuong);
        }
        ///Tính tổng thành tiền
        public decimal TongTT()
        {
            return List_SP.Sum(x => x.ThanhTien);
        }

        ///Them sp
        /// Trả về:
        ///  1  => thành công
        /// -1  => sản phẩm không tồn tại
        /// -2  => không đủ tồn kho
        public int ThemSP(int id, int soLuong)
        {
            // Lấy thông tin sản phẩm từ DB để kiểm tra tồn kho
            var spDb = data.tblSanPham.FirstOrDefault(x => x.MaSP == id);
            if (spDb == null)
                return -1;

            int available = spDb.SoLuongTon ?? 0;
            CartItem item = List_SP.Find(x => x.MaSP == id);

            int newQty = (item != null) ? item.soLuong + soLuong : soLuong;

            if (newQty > available)
            {
                // Không đủ tồn kho
                return -2;
            }

            if (item != null)
            {
                item.soLuong = newQty;
            }
            else
            {
                CartItem sp = new CartItem(id, soLuong);
                if (sp == null)
                    return -1;
                List_SP.Add(sp);
            }

            return 1;
        }

        public int XoaSP(int id)
        {
            CartItem item = new CartItem();
            item = List_SP.FirstOrDefault(x => x.MaSP == id);
            if (item != null)//nếu sản phẩm đã có trong giỏ
            {
                List_SP.Remove(item);
                return 1;//Xóa thành công!
            }
            else
            {
                return -1;//Xóa không thành công!
            }

        }

        /// Cập nhật số lượng
        /// thaotac: 1 => tăng, 2 => giảm
        /// Trả về:
        ///  1 => thành công
        /// -1 => sản phẩm không tồn tại trong giỏ
        /// -2 => không đủ tồn kho khi tăng
        public int CapNhatSL(int id, int thaotac)
        {
            CartItem item = new CartItem();
            item = List_SP.FirstOrDefault(x => x.MaSP == id);
            if (item == null)
                return -1; //thực hiện thao tác thất bại!
            else
            {
                if (thaotac == 1)
                {
                    // Kiểm tra tồn kho trước khi tăng
                    var spDb = data.tblSanPham.FirstOrDefault(x => x.MaSP == id);
                    int available = spDb?.SoLuongTon ?? 0;
                    if (item.soLuong + 1 > available)
                        return -2; // không đủ tồn kho

                    item.soLuong++;
                }
                else if (thaotac == 2)
                {
                    if (item.soLuong == 1)
                        List_SP.Remove(item);
                    else
                        item.soLuong--;
                }
                return 1;
            }
        }
    }
}