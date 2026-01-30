using Microsoft.AspNetCore.Identity;

namespace MedicineLog.Data.Entities
{
    public sealed class AppUser : IdentityUser
    {
        public AppUser()
        {
        }

        public AppUser(string email)
        {
            Email = UserName = email;
        }
        // Auditor can be granted access to multiple sites
        public ICollection<AuditorSiteAccess> AuditorSiteAccesses { get; set; } = new List<AuditorSiteAccess>();
    }

}
