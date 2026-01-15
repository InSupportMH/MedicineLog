using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MedicineLog.Data.Entities
{
    public sealed class Site
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<Terminal> Terminals { get; set; } = new List<Terminal>();
        public ICollection<MedicineLogEntry> LogEntries { get; set; } = new List<MedicineLogEntry>();

        public ICollection<AuditorSiteAccess> AuditorAccess { get; set; } = new List<AuditorSiteAccess>();
    }

}
