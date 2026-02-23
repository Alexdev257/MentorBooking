using Shared.Kernel.Domain;

namespace AuthService.Domain.Entities
{
    public class Teacher : AuditableEntity
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Department { get; set; }
        public string? Specialization { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid? CreatedByAdminId { get; set; }
        public virtual User User { get; set; } = default!;
    }
}
