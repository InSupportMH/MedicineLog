namespace MedicineLog.Models
{
    using System.ComponentModel.DataAnnotations;

    public class MedicineRegViewModel
    {
        [Display(Name = "Förnamn")]
        [Required(ErrorMessage = "Förnamn är obligatoriskt.")]
        [StringLength(100)]
        public string FirstName { get; set; } = "";

        [Display(Name = "Efternamn")]
        [Required(ErrorMessage = "Efternamn är obligatoriskt.")]
        [StringLength(100)]
        public string LastName { get; set; } = "";

        [Display(Name = "Läkemedel")]
        [Required(ErrorMessage = "Läkemedlets namn är obligatoriskt.")]
        [StringLength(200)]
        public string MedicationName { get; set; } = "";

        [Display(Name = "Antal")]
        [Required(ErrorMessage = "Antal är obligatoriskt.")]
        [Range(1, 100000, ErrorMessage = "Antal måste vara minst 1.")]
        public int? Quantity { get; set; }
    }

}
