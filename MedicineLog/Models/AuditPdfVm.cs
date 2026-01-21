using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MedicineLog.ViewModels;

public sealed class AuditPdfVm
{
    [Display(Name = "Plats")]
    [Required(ErrorMessage = "Välj en plats.")]
    public int SiteId { get; set; }
}
