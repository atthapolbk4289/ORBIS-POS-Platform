using PosSystem.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosSystem.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(Guid id);
        Task<List<User>> GetByBranchAsync(Guid branchId, string? roleFilter = null, string? search = null);
        Task CreateAsync(User user);
        Task UpdateLastLoginAsync(Guid userId);
        Task LogAuditAsync(Guid? userId, Guid? branchId, string action, string? entityType = null, string? entityId = null, string? ipAddress = null);
        Task SaveSessionAsync(Guid userId, string sessionKey, string? ipAddress, string? deviceInfo, DateTime expiresAt);
    }
}
