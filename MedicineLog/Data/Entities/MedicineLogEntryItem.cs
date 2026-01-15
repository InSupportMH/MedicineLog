using System;
using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Data.Entities
{
    public sealed class MedicineLogEntryItem
    {
        public int Id { get; set; }

        public int LogEntryId { get; set; }
        public MedicineLogEntry LogEntry { get; set; } = null!;

        [Required, StringLength(200)]
        public string MedicineName { get; set; } = "";
        [Range(1, 100000)]
        public int Quantity { get; set; }
    }

}
