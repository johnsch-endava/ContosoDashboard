using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public interface IFileScreeningService
{
    Task<FileScreeningResult> ScreenAsync(string fileName, string mimeType, long fileSizeBytes, CancellationToken cancellationToken = default);
}

public class ValidationFileScreeningService : IFileScreeningService
{
    private readonly DocumentStorageOptions _options;

    public ValidationFileScreeningService(IOptions<DocumentStorageOptions> options)
    {
        _options = options.Value;
    }

    public Task<FileScreeningResult> ScreenAsync(string fileName, string mimeType, long fileSizeBytes, CancellationToken cancellationToken = default)
    {
        if (fileSizeBytes <= 0)
        {
            return Task.FromResult(FileScreeningResult.Failed("File is empty."));
        }

        if (fileSizeBytes > _options.MaxFileSizeBytes)
        {
            return Task.FromResult(FileScreeningResult.Failed($"File exceeds the maximum size of {_options.MaxFileSizeBytes / (1024 * 1024)} MB."));
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Task.FromResult(FileScreeningResult.Failed("File type is not supported."));
        }

        if (!_options.AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
        {
            return Task.FromResult(FileScreeningResult.Failed("File content type is not supported."));
        }

        return Task.FromResult(FileScreeningResult.Passed());
    }
}

public class FileScreeningResult
{
    public bool IsAllowed { get; init; }

    public string Outcome { get; init; } = "Passed";

    public static FileScreeningResult Passed() => new() { IsAllowed = true, Outcome = "Passed" };

    public static FileScreeningResult Failed(string outcome) => new() { IsAllowed = false, Outcome = outcome };
}

public class DocumentStorageOptions
{
    public string RootPath { get; set; } = "AppData/uploads";

    public long MaxFileSizeBytes { get; set; } = 26214400;

    public List<string> AllowedExtensions { get; set; } = new();

    public List<string> AllowedMimeTypes { get; set; } = new();
}

public class DocumentScanningOptions
{
    public string Mode { get; set; } = "OfflineValidation";

    public bool EnableQueueDispatch { get; set; }

    public string DefaultScanStatus { get; set; } = "Passed";
}

public class DocumentScanQueueOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "document-scan-requests";
}