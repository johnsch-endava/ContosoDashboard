namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<FileStorageSaveResult> SaveAsync(string userSegment, string projectSegment, string originalFileName, Stream fileStream, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

public class FileStorageSaveResult
{
    public string StoredFileName { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string FileExtension { get; init; } = string.Empty;
}