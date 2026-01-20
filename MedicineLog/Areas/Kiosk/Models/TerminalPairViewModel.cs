using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Areas.Kiosk.Models
{
    public sealed class TerminalPairViewModel
    {
        [Required(ErrorMessage = "Ange en parkod.")]
        [StringLength(50)]
        [Display(Name = "Parkod")]
        public string Code { get; set; } = "";
    }
}
