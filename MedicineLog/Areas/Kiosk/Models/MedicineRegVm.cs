namespace MedicineLog.Areas.Kiosk.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class MedicineItemVm
    {
        [Display(Name = "Läkemedel")]
        [Required(ErrorMessage = "Läkemedlets namn saknas.")]
        [StringLength(200)]
        public string MedicineName { get; set; } = "";

        [Display(Name = "Antal")]
        [Required(ErrorMessage = "Antal saknas.")]
        [Range(1, 1000, ErrorMessage = "Gitligt antal: 1 - 1000.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Bild saknas.")]
        public IFormFile Photo { get; set; } = default!;
    }

    public class MedicineRegVm
    {
        [Display(Name = "Förnamn")]
        [Required(ErrorMessage = "Förnamn saknas.")]
        [StringLength(100)]
        public string FirstName { get; set; } = "";

        [Display(Name = "Efternamn")]
        [Required(ErrorMessage = "Efternamn saknas.")]
        [StringLength(100)]
        public string LastName { get; set; } = "";

        [Display(Name = "Läkemedel")]
        [MinLength(1, ErrorMessage = "Lägg till minst ett läkemedel.")]
        public List<MedicineItemVm> Medicines { get; set; } = new()
        {
            new MedicineItemVm()
        };
    }
}
