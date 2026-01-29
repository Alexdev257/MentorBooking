using SharedKernel.Domain;

namespace AuthService.Domain.Entities
{
    public class UserLocation : AuditableEntity
    {
        public decimal? Longitude { get; set; } = 0;
        public decimal? Latitude { get; set; } = 0;
        public Guid? UserId { get; set; }
        public virtual User? User { get; set; }
    }
}