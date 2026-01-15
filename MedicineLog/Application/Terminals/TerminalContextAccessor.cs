namespace MedicineLog.Application.Terminals
{
    public sealed class TerminalContextAccessor : ITerminalContextAccessor
    {
        public TerminalContext? Current { get; set; }
    }
}
