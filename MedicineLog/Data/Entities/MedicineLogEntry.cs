using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicineLog.Data.Entities
{
    public sealed class MedicineLogEntry
    {
        public int Id { get; set; }

        public int SiteId { get; set; }
        public Site Site { get; set; } = null!;

        public int TerminalId { get; set; }
        public Terminal Terminal { get; set; } = null!;

        [Required, StringLength(100)]
        public string FirstName { get; set; } = "";
        [Required, StringLength(100)]
        public string LastName { get; set; } = "";
        public ICollection<MedicineLogEntryItem> Items { get; set; } = new List<MedicineLogEntryItem>();

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

}
