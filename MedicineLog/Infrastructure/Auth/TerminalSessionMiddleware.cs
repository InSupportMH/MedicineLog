using MedicineLog.Application.Terminals;
using MedicineLog.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicineLog.Infrastructure.Auth
{
    public sealed class TerminalSessionMiddleware
    {
        public const string TokenName = "ml_terminal";
        private readonly RequestDelegate _next;

        public TerminalSessionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext http,
            AppDbContext db,
            ITerminalContextAccessor terminalCtx)
        {
            // cookie contains the raw refresh token (or some session key)
            var token = http.Request.Cookies[TokenName];

            if (!string.IsNullOrWhiteSpace(token))
            {
                var tokenHash = TokenHelper.Sha256Base64(token);

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
    }
}
