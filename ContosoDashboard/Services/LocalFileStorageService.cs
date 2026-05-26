using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly DocumentStorageOptions _options;

    public LocalFileStorageService(IWebHostEnvironment environment, IOptions<DocumentStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<FileStorageSaveResult> SaveAsync(string userSegment, string projectSegment, string originalFileName, Stream fileStream, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var rootPath = GetAbsoluteRootPath();
        var relativePath = Path.Combine(userSegment, projectSegment, storedFileName);
        var absolutePath = Path.Combine(rootPath, relativePath);

        var directoryPath = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        await using var output = File.Create(absolutePath);
        await fileStream.CopyToAsync(output, cancellationToken);

        return new FileStorageSaveResult
        {
            StoredFileName = storedFileName,
            RelativePath = NormalizeRelativePath(relativePath),
            FileExtension = extension
        };
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(GetAbsoluteRootPath(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(GetAbsoluteRootPath(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private string GetAbsoluteRootPath()
    {
        var configuredRoot = _options.RootPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_environment.ContentRootPath, configuredRoot);
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }
}