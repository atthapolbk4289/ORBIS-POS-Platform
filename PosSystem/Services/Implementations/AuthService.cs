using PosSystem.Models.Dtos;
using PosSystem.Repositories.Interfaces;
using PosSystem.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PosSystem.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<LoginResult> LoginAsync(string username, string password, string ipAddress, string userAgent)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            
            if (user == null || user.Status != "ACTIVE")
            {
                if (user != null)
                {
                    await _userRepository.LogAuditAsync(user.Id, user.BranchId, "LOGIN_FAILED", ipAddress: ipAddress);
                }
                return new LoginResult { Success = false, Message = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง หรือบัญชีถูกระงับ" };
            }

            bool isValid = false;
            try 
            {
                // In a real system, you might not use prefix for testing like we did in the seed script
                // We'll handle placeholder passwords just for the initial login after seeding
                if (user.PasswordHash.StartsWith("HASH_"))
                {
                    isValid = (password == "Admin@1234"); // Simple bypass for testing seeded data
                }
                else
                {
                    isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }
            }
            catch
            {
                isValid = false;
            }

            if (!isValid)
            {
                await _userRepository.LogAuditAsync(user.Id, user.BranchId, "LOGIN_FAILED", ipAddress: ipAddress);
                return new LoginResult { Success = false, Message = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง" };
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("BranchId", user.BranchId.ToString()),
                new Claim("BranchName", user.BranchName ?? "")
            };

            var identity = new ClaimsIdentity(claims, "Cookie");
            var principal = new ClaimsPrincipal(identity);

            // Save Session
            var sessionKey = Guid.NewGuid().ToString("N");
            await _userRepository.SaveSessionAsync(user.Id, sessionKey, ipAddress, userAgent, DateTime.UtcNow.AddHours(8));
            await _userRepository.UpdateLastLoginAsync(user.Id);
            await _userRepository.LogAuditAsync(user.Id, user.BranchId, "LOGIN_SUCCESS", ipAddress: ipAddress);

            return new LoginResult 
            { 
                Success = true, 
                Principal = principal,
                User = user
            };
        }
    }
}
