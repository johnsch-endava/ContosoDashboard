using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public class NoOpFileScanDispatchService : IFileScanDispatchService
{
    public Task DispatchAsync(DocumentScanRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}