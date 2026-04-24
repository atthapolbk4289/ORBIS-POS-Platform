using System.Security.Claims;

namespace PosSystem.Models.Dtos
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public ClaimsPrincipal? Principal { get; set; }
        public object? User { get; set; }
    }
}
