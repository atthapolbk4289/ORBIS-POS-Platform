using Microsoft.Data.SqlClient;
using PosSystem.Helpers;
using PosSystem.Models.Entities;
using PosSystem.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosSystem.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly ISqlHelper _sql;

        public UserRepository(ISqlHelper sql)
        {
            _sql = sql;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _sql.QueryFirstOrDefaultAsync<User>(
                "SELECT u.*, b.Name AS BranchName FROM Users u JOIN Branches b ON u.BranchId = b.Id WHERE u.Username = @Username",
                new[] { new SqlParameter("@Username", username) });
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _sql.QueryFirstOrDefaultAsync<User>(
                "SELECT u.*, b.Name AS BranchName FROM Users u JOIN Branches b ON u.BranchId = b.Id WHERE u.Id = @Id",
                new[] { new SqlParameter("@Id", id) });
        }

        public async Task<List<User>> GetByBranchAsync(Guid branchId, string? roleFilter = null, string? search = null)
        {
            var sql = @"SELECT Id, BranchId, Username, FullName, Role, Status, Phone, Email, LastLoginAt, CreatedAt
                        FROM Users WHERE BranchId = @BranchId
                        AND (@Role IS NULL OR Role = @Role)
                        AND (@Search IS NULL OR FullName LIKE '%' + @Search + '%' OR Username LIKE '%' + @Search + '%')
                        ORDER BY FullName";
            
            return await _sql.QueryAsync<User>(sql, new[] {
                new SqlParameter("@BranchId", branchId),
                new SqlParameter("@Role", (object?)roleFilter ?? DBNull.Value),
                new SqlParameter("@Search", (object?)search ?? DBNull.Value)
            });
        }

        public async Task CreateAsync(User user)
        {
            await _sql.ExecuteAsync(
                @"INSERT INTO Users (Id, BranchId, Username, PasswordHash, FullName, Role, Status, Phone, Email, Pin)
                  VALUES (@Id, @BranchId, @Username, @PasswordHash, @FullName, @Role, @Status, @Phone, @Email, @Pin)",
                new[] {
                    new SqlParameter("@Id", user.Id),
                    new SqlParameter("@BranchId", user.BranchId),
                    new SqlParameter("@Username", user.Username),
                    new SqlParameter("@PasswordHash", user.PasswordHash),
                    new SqlParameter("@FullName", user.FullName),
                    new SqlParameter("@Role", user.Role),
                    new SqlParameter("@Status", user.Status),
                    new SqlParameter("@Phone", (object?)user.Phone ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)user.Email ?? DBNull.Value),
                    new SqlParameter("@Pin", (object?)user.Pin ?? DBNull.Value)
                });
        }

        public async Task UpdateLastLoginAsync(Guid userId)
        {
            await _sql.ExecuteAsync(
                "UPDATE Users SET LastLoginAt = GETUTCDATE() WHERE Id = @Id",
                new[] { new SqlParameter("@Id", userId) });
        }

        public async Task LogAuditAsync(Guid? userId, Guid? branchId, string action, string? entityType = null, string? entityId = null, string? ipAddress = null)
        {
            await _sql.ExecuteAsync(
                @"INSERT INTO AuditLogs (Id, UserId, BranchId, Action, EntityType, EntityId, IpAddress)
                  VALUES (NEWID(), @UserId, @BranchId, @Action, @EntityType, @EntityId, @IpAddress)",
                new[] {
                    new SqlParameter("@UserId", (object?)userId ?? DBNull.Value),
                    new SqlParameter("@BranchId", (object?)branchId ?? DBNull.Value),
                    new SqlParameter("@Action", action),
                    new SqlParameter("@EntityType", (object?)entityType ?? DBNull.Value),
                    new SqlParameter("@EntityId", (object?)entityId ?? DBNull.Value),
                    new SqlParameter("@IpAddress", (object?)ipAddress ?? DBNull.Value)
                });
        }

        public async Task SaveSessionAsync(Guid userId, string sessionKey, string? ipAddress, string? deviceInfo, DateTime expiresAt)
        {
            await _sql.ExecuteAsync(
                @"INSERT INTO UserSessions (Id, UserId, SessionKey, IpAddress, DeviceInfo, ExpiresAt)
                  VALUES (NEWID(), @UserId, @SessionKey, @IpAddress, @DeviceInfo, @ExpiresAt)",
                new[] {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@SessionKey", sessionKey),
                    new SqlParameter("@IpAddress", (object?)ipAddress ?? DBNull.Value),
                    new SqlParameter("@DeviceInfo", (object?)deviceInfo ?? DBNull.Value),
                    new SqlParameter("@ExpiresAt", expiresAt)
                });
        }
    }
}
