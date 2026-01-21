namespace MedicineLog.Models
{
    public class ErrorVm
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
