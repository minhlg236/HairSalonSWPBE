using static System.Runtime.InteropServices.JavaScript.JSType;

namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class LevelAccount
    {
        public int Id { get; set; } // Đặt tên thuộc tính theo PascalCase để tuân theo chuẩn C#
        public string Title { get; set; }
        public DateTime CreateOn { get; set; } // Sử dụng DateTime thay cho date
        public DateTime UpdateOn { get; set; } // Sử dụng DateTime thay cho date


    }
}
