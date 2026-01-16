using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

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
