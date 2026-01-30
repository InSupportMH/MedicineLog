using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Data.Entities
{
    public sealed class AuditorSiteAccess
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;

        public int SiteId { get; set; }
        public Site Site { get; set; } = null!;

        public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    }

}
