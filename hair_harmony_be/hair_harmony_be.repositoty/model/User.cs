using static System.Runtime.InteropServices.JavaScript.JSType;

namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class User
    {
        public int Id { get; set; } // Sử dụng PascalCase để tuân theo chuẩn C#
        public string FullName { get; set; } // fullName đổi thành PascalCase
        public string UserName { get; set; } // Đổi kiểu từ DateTime sang string
        public bool Gender { get; set; } // Boolean đổi thành bool (kiểu chuẩn của C#)
        public string Password { get; set; } // Boolean đổi thành bool (kiểu chuẩn của C#)

        public DateTime Dob { get; set; } // dob đổi thành PascalCase
        public string Address { get; set; }
        public float SalaryStaff { get; set; } // salary_staff đổi thành PascalCase
        public LevelAccount LevelAccount { get; set; }
        public Role Role { get; set; }
        public DateTime CreateOn { get; set; }
        public DateTime UpdateOn { get; set; }
    }
}
