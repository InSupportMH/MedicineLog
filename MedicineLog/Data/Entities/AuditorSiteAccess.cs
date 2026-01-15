using System;

namespace MedicineLog.Data.Entities
{
    public sealed class AuditorSiteAccess
    {
        public string UserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;

        public int SiteId { get; set; }
        public Site Site { get; set; } = null!;

        public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    }

}
