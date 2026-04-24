using System.ComponentModel.DataAnnotations;

namespace PosSystem.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        public string Password { get; set; } = null!;

        public bool RememberMe { get; set; }
    }
}
