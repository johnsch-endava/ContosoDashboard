using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IFileScanDispatchService
{
    Task DispatchAsync(DocumentScanRequest request, CancellationToken cancellationToken = default);
}