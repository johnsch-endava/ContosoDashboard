using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<DocumentListResponse> GetDocumentsAsync(int requestingUserId, string? department, DocumentQueryRequest query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentListItem>> GetProjectDocumentsAsync(int projectId, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentListItem>> GetTaskDocumentsAsync(int taskId, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<DocumentDetailsResponse?> GetDocumentByIdAsync(int documentId, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<DocumentDetailsResponse> UploadDocumentAsync(DocumentUploadRequest request, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<DocumentDetailsResponse?> UpdateDocumentAsync(int documentId, DocumentUpdateRequest request, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<DocumentDetailsResponse?> ReplaceDocumentAsync(int documentId, DocumentUploadRequest request, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<DocumentShareResponse?> ShareDocumentAsync(int documentId, DocumentShareRequest request, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<DocumentFileResponse?> DownloadDocumentAsync(int documentId, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<DocumentFileResponse?> PreviewDocumentAsync(int documentId, int requestingUserId, string? department, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentListItem>> GetRecentDocumentsAsync(int requestingUserId, string? department, int count, CancellationToken cancellationToken = default);

    Task<int> GetAccessibleDocumentCountAsync(int requestingUserId, string? department, CancellationToken cancellationToken = default);
}