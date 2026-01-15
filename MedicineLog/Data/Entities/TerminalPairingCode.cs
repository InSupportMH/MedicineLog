using System;
using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Data.Entities
{
    public sealed class TerminalPairingCode
    {
        [Key]
        public int Id { get; set; }

        public int TerminalId { get; set; }
        public Terminal Terminal { get; set; } = null!;

        // What the admin shows / iPad enters or scans (store a hash if you want extra safety)
        public string Code { get; set; } = null!;

        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UsedAt { get; set; }
        public string? UsedByIpAddress { get; set; }
    }

}
