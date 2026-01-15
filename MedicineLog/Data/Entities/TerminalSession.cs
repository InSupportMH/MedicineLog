using System;

namespace MedicineLog.Data.Entities
{
    public sealed class TerminalSession
    {
        public int Id { get; set; }

        public int TerminalId { get; set; }
        public Terminal Terminal { get; set; } = null!;

        // Store only a hash of the token presented by the device
        public string RefreshTokenHash { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? RevokedAt { get; set; }
        public string? CreatedByIpAddress { get; set; }
        public string? UserAgent { get; set; }

        public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;
    }

}
