namespace MedicineLog.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class MedicineItemViewModel
    {
        [Display(Name = "Läkemedel")]
        [Required(ErrorMessage = "Läkemedlets namn är obligatoriskt.")]
        [StringLength(200)]
        public string MedicineName { get; set; } = "";

        [Display(Name = "Antal")]
        [Required(ErrorMessage = "Antal är obligatoriskt.")]
        [Range(1, 100000, ErrorMessage = "Antal måste vara minst 1.")]
        public int? Quantity { get; set; }
    }

    public class MedicineRegVm
    {
        public int SiteId { get; set; }
        public int TerminalId { get; set; }

        [Display(Name = "Förnamn")]
        [Required(ErrorMessage = "Förnamn är obligatoriskt.")]
        [StringLength(100)]
        public string FirstName { get; set; } = "";

        [Display(Name = "Efternamn")]
        [Required(ErrorMessage = "Efternamn är obligatoriskt.")]
        [StringLength(100)]
        public string LastName { get; set; } = "";

        [Display(Name = "Läkemedel")]
        [MinLength(1, ErrorMessage = "Lägg till minst ett läkemedel.")]
        public List<MedicineItemViewModel> Medicines { get; set; } = new()
        {
            new MedicineItemViewModel()
        };
    }
}
