using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Data.Entities
{
    public sealed class MedicineLogEntryItem
    {
        [Key]
        public int Id { get; set; }

        public int LogEntryId { get; set; }
        public MedicineLogEntry LogEntry { get; set; } = null!;

        [Required, StringLength(200)]
        public string MedicineName { get; set; } = "";
        [Range(1, 10000)]
        public int Quantity { get; set; }

        public string PhotoPath { get; set; } = "";
        public string PhotoContentType { get; set; } = "";
        public long PhotoLength { get; set; }
    }

}
