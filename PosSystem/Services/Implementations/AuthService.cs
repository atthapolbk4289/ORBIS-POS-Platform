using PosSystem.Models.Dtos;
using PosSystem.Repositories.Interfaces;
using PosSystem.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PosSystem.Services.Implementations
{
    /// <summary>
    /// เซอร์วิสสำหรับจัดการการยืนยันตัวตน (Authentication)
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// ตรวจสอบการเข้าสู่ระบบด้วยชื่อผู้ใช้และรหัสผ่าน
        /// </summary>
        /// <param name="username">ชื่อผู้ใช้งาน</param>
        /// <param name="password">รหัสผ่าน</param>
        /// <param name="ipAddress">ที่อยู่ IP ของผู้ใช้งาน</param>
        /// <param name="userAgent">ข้อมูลเบราว์เซอร์ของผู้ใช้งาน</param>
        /// <returns>ผลลัพธ์การเข้าสู่ระบบพร้อมข้อมูล Claims</returns>
        public async Task<LoginResult> LoginAsync(string username, string password, string ipAddress, string userAgent)
        {
            // ดึงข้อมูลผู้ใช้งานตามชื่อผู้ใช้
            var user = await _userRepository.GetByUsernameAsync(username);
            
            // ตรวจสอบว่ามีผู้ใช้งานหรือไม่ และสถานะบัญชีเป็น ACTIVE หรือไม่
            if (user == null || user.Status != "ACTIVE")
            {
                if (user != null)
                {
                    // บันทึก Log กรณีล็อกอินไม่สำเร็จ (บัญชีไม่พร้อมใช้งาน)
                    await _userRepository.LogAuditAsync(user.Id, user.BranchId, "LOGIN_FAILED", ipAddress: ipAddress);
                }
                return new LoginResult { Success = false, Message = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง หรือบัญชีถูกระงับ" };
            }

            bool isValid = false;
            try 
            {
                // ตรวจสอบรหัสผ่าน (รองรับทั้งแบบ Seeded Data และ BCrypt)
                if (user.PasswordHash.StartsWith("HASH_"))
                {
                    isValid = (password == "Admin@1234"); // ข้ามการตรวจสอบสำหรับข้อมูลทดสอบ
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

            // ถ้ารหัสผ่านไม่ถูกต้อง
            if (!isValid)
            {
                await _userRepository.LogAuditAsync(user.Id, user.BranchId, "LOGIN_FAILED", ipAddress: ipAddress);
                return new LoginResult { Success = false, Message = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง" };
            }

            // เตรียมข้อมูล Claims สำหรับเก็บไว้ใน Cookie
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

            // บันทึก Session ลงฐานข้อมูล
            var sessionKey = Guid.NewGuid().ToString("N");
            await _userRepository.SaveSessionAsync(user.Id, sessionKey, ipAddress, userAgent, DateTime.UtcNow.AddHours(8));
            
            // อัปเดตเวลาเข้าใช้งานล่าสุด
            await _userRepository.UpdateLastLoginAsync(user.Id);
            
            // บันทึก Log การเข้าสู่ระบบสำเร็จ
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

