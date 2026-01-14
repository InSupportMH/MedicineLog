using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicineLog.Data
{
    public class ApplicationUser : IdentityUser
    {
        

        public ApplicationUser()
        {
        }

        public ApplicationUser(string email)
        {
            UserName = Email = email;
        }
    }
}
