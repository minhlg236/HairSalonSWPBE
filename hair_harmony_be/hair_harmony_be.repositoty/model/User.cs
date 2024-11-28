
using System.Text.Json.Serialization;
namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class User
    {
        public int Id { get; set; } // Sử dụng PascalCase để tuân theo chuẩn C#
        public string FullName { get; set; } // fullName đổi thành PascalCase
        public string UserName { get; set; } // Đổi kiểu từ DateTime sang string
        public bool Gender { get; set; } // Boolean đổi thành bool (kiểu chuẩn của C#)
        public string? Email { get; set; }
        [JsonIgnore] // Bỏ qua khi trả về trong API
         public string Password { get; set; }
        public DateTime? Dob { get; set; } // dob đổi thành PascalCase
        public string? Address { get; set; }
        public Role Role { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool Status { get; set; }

        public DateTime UpdatedOn { get; set; }

    }

    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string RoleName { get; set; }
        public bool Status { get; set; }
    }
}
