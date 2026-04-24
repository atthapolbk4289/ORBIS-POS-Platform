using System;

namespace PosSystem.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; } = null!;
        public string Status { get; set; } = "ACTIVE";
        public string? Pin { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Extended property
        public string? BranchName { get; set; }
    }
}
