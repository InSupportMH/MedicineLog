namespace MedicineLog.Services
{
    public interface IPhotoStoreService
    {
        Task<string> SaveAsync(IFormFile file, string subfolder, CancellationToken ct);
        Task DeleteAsync(string storedPath, CancellationToken ct);
    }
}
