using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Models
{
    public sealed class LoginVm
    {
        [Required(ErrorMessage = "E-post krävs.")]
        [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
        [Display(Name = "E-post")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Lösenord krävs.")]
        [DataType(DataType.Password)]
        [Display(Name = "Lösenord")]
        public string Password { get; set; } = "";

        [Display(Name = "Kom ihåg mig")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
