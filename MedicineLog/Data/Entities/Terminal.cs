using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Data.Entities
{
    public sealed class Terminal
    {
        [Key]
        public int Id { get; set; }

        public int SiteId { get; set; }
        public Site Site { get; set; } = null!;

        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        // For auditing / monitoring
        public DateTimeOffset? LastSeenAt { get; set; }

        public ICollection<MedicineLogEntry> LogEntries { get; set; } = new List<MedicineLogEntry>();

        // Device auth (recommended: store sessions/tokens separately, not a shared password)
        public ICollection<TerminalSession> Sessions { get; set; } = new List<TerminalSession>();
    }

}
