using Shared.Kernel.Domain;

namespace AuthService.Domain.Entities
{
    public class Student : AuditableEntity
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? StudentCode { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid? CreatedByAdminId { get; set; }
        public virtual User User { get; set; } = default!;
    }
}
