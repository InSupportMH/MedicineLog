using MedicineLog.Application.Terminals;
using MedicineLog.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicineLog.Infrastructure.Auth
{
    public sealed class TerminalSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public TerminalSessionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext http,
            AppDbContext db,
            ITerminalContextAccessor terminalCtx)
        {
            // cookie contains the raw refresh token (or some session key)
            var token = http.Request.Cookies["ml_terminal"];

            if (!string.IsNullOrWhiteSpace(token))
            {
                var tokenHash = HashToken(token); // your hashing method

                var session = await db.TerminalSessions
                    .Include(s => s.Terminal)
                    .FirstOrDefaultAsync(s => s.RefreshTokenHash == tokenHash);

                if (session?.IsActive == true)
                {
                    terminalCtx.Current = new TerminalContext(
                        session.TerminalId,
                        session.Terminal.SiteId
                    );
                }
            }

            await _next(http);
        }

        private static string HashToken(string token)
        {
            // e.g. SHA256 + base64; keep it consistent with the stored hash
            throw new NotImplementedException();
        }
    }
}
