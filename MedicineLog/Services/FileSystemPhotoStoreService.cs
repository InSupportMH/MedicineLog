namespace MedicineLog.Services
{
    public sealed class FileSystemPhotoStoreService : IPhotoStoreService
    {
        private readonly string _root;
        private readonly ILogger<FileSystemPhotoStoreService> _logger;

        public FileSystemPhotoStoreService(IConfiguration cfg, ILogger<FileSystemPhotoStoreService> log)
        {
            _root = cfg["PhotoStore:Root"] ?? throw new InvalidOperationException("PhotoStore:Root missing");
            _logger = log;
        }

        public async Task<string> SaveAsync(IFormFile file, string subfolder, CancellationToken ct)
        {
            Directory.CreateDirectory(_root);

            // Basic validation (add more as needed)
            if (file.Length <= 0) throw new InvalidOperationException("Empty file.");
            if (file.Length > 5_000_000) throw new InvalidOperationException("File too large."); // example 5 MB

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";

            // Optional: only allow image types
            var ctType = file.ContentType?.ToLowerInvariant() ?? "";
            if (ctType != "image/jpeg" && ctType != "image/png" && ctType != "image/webp")
                throw new InvalidOperationException("Invalid image type.");

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var relDir = subfolder.Trim('/', '\\');
            var relPath = Path.Combine(relDir, fileName).Replace('\\', '/');

            var absDir = Path.Combine(_root, relDir);
            Directory.CreateDirectory(absDir);

            var absPath = Path.Combine(absDir, fileName);

            // Safer write: temp then move
            var tmpPath = absPath + ".tmp";

            await using (var fs = new FileStream(tmpPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
            {
                await file.CopyToAsync(fs, ct);
            }

            File.Move(tmpPath, absPath);

            return relPath;
        }

        public Task DeleteAsync(string storedPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return Task.CompletedTask;

            var absPath = Path.Combine(_root, storedPath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (File.Exists(absPath)) File.Delete(absPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed deleting photo {Path}", absPath);
            }

            return Task.CompletedTask;
        }
    }
}
