namespace MedicineLog.Application.Terminals
{
    public interface ITerminalContextAccessor
    {
        TerminalContext? Current { get; set; }
    }
}
