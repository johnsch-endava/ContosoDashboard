using System.Security.Claims;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/documents")
            .RequireAuthorization();

        group.MapGet("", async (HttpContext context, IDocumentService documentService, CancellationToken cancellationToken, string? scope, int? projectId, int? taskId, string? category, string? search, DateTime? fromDate, DateTime? toDate, string? sort, string? direction) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await documentService.GetDocumentsAsync(userId.Value, GetDepartment(context.User), new DocumentQueryRequest
            {
                Scope = scope ?? "my",
                ProjectId = projectId,
                TaskId = taskId,
                Category = category,
                Search = search,
                FromDate = fromDate,
                ToDate = toDate,
                Sort = sort ?? "uploadDate",
                Direction = direction ?? "desc"
            }, cancellationToken);

            return Results.Ok(result);
        });

        group.MapPost("", async (HttpContext context, HttpRequest request, IDocumentService documentService, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null)
            {
                return Results.BadRequest("A file is required.");
            }

            await using var fileStream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var tagValues = form.TryGetValue("tags", out var uploadTags)
                ? uploadTags.SelectMany(value => value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>()).ToList()
                : new List<string>();

            var uploadRequest = new DocumentUploadRequest
            {
                Title = form["title"].ToString(),
                Category = form["category"].ToString(),
                Description = form["description"].ToString(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                FileContent = memoryStream,
                Tags = tagValues
            };

            if (int.TryParse(form["projectId"], out var projectId))
            {
                uploadRequest.ProjectId = projectId;
            }

            if (int.TryParse(form["taskId"], out var taskId))
            {
                uploadRequest.TaskId = taskId;
            }

            try
            {
                var document = await documentService.UploadDocumentAsync(uploadRequest, userId.Value, GetDepartment(context.User), cancellationToken);
                return Results.Created($"/api/documents/{document.DocumentId}", document);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(exception.Message);
            }
        });

        group.MapGet("/{documentId:int}", async (HttpContext context, IDocumentService documentService, int documentId, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var document = await documentService.GetDocumentByIdAsync(documentId, userId.Value, GetDepartment(context.User), cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        });

        group.MapPatch("/{documentId:int}", async (HttpContext context, IDocumentService documentService, int documentId, DocumentUpdateRequest request, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var document = await documentService.UpdateDocumentAsync(documentId, request, userId.Value, GetDepartment(context.User), cancellationToken);
                return document is null ? Results.Forbid() : Results.Ok(document);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(exception.Message);
            }
        });

        group.MapDelete("/{documentId:int}", async (HttpContext context, IDocumentService documentService, int documentId, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var deleted = await documentService.DeleteDocumentAsync(documentId, userId.Value, GetDepartment(context.User), cancellationToken);
            return deleted ? Results.NoContent() : Results.Forbid();
        });

        group.MapGet("/{documentId:int}/download", async (HttpContext context, IDocumentService documentService, int documentId, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var file = await documentService.DownloadDocumentAsync(documentId, userId.Value, GetDepartment(context.User), cancellationToken);
            return file is null ? Results.Forbid() : Results.File(file.Stream, file.MimeType, file.FileName);
        });

        group.MapGet("/{documentId:int}/preview", async (HttpContext context, IDocumentService documentService, int documentId, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var file = await documentService.PreviewDocumentAsync(documentId, userId.Value, GetDepartment(context.User), cancellationToken);
                return file is null ? Results.Forbid() : Results.File(file.Stream, file.MimeType, enableRangeProcessing: true);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(exception.Message);
            }
        });

        group.MapPost("/{documentId:int}/replace", async (HttpContext context, HttpRequest request, IDocumentService documentService, int documentId, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null)
            {
                return Results.BadRequest("A file is required.");
            }

            await using var fileStream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            try
            {
                var replacementTags = form.TryGetValue("tags", out var replaceTags)
                    ? replaceTags.SelectMany(value => value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>()).ToList()
                    : new List<string>();

                var document = await documentService.ReplaceDocumentAsync(documentId, new DocumentUploadRequest
                {
                    Title = form["title"].ToString(),
                    Category = form["category"].ToString(),
                    Description = form["description"].ToString(),
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    FileContent = memoryStream,
                    Tags = replacementTags
                }, userId.Value, GetDepartment(context.User), cancellationToken);

                return document is null ? Results.Forbid() : Results.Ok(document);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(exception.Message);
            }
        });

        group.MapPost("/{documentId:int}/shares", async (HttpContext context, IDocumentService documentService, int documentId, DocumentShareRequest request, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var share = await documentService.ShareDocumentAsync(documentId, request, userId.Value, GetDepartment(context.User), cancellationToken);
                return share is null ? Results.Forbid() : Results.Created($"/api/documents/{documentId}/shares/{share.DocumentShareId}", share);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(exception.Message);
            }
        });

        endpoints.MapGet("/api/projects/{projectId:int}/documents", async (HttpContext context, IDocumentService documentService, int projectId, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var documents = await documentService.GetProjectDocumentsAsync(projectId, userId.Value, GetDepartment(context.User), cancellationToken);
            return Results.Ok(new DocumentListResponse
            {
                Items = documents,
                TotalCount = documents.Count,
                AppliedScope = "accessible"
            });
        }).RequireAuthorization();

        endpoints.MapGet("/api/tasks/{taskId:int}/documents", async (HttpContext context, IDocumentService documentService, int taskId, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var documents = await documentService.GetTaskDocumentsAsync(taskId, userId.Value, GetDepartment(context.User), cancellationToken);
            return Results.Ok(new DocumentListResponse
            {
                Items = documents,
                TotalCount = documents.Count,
                AppliedScope = "accessible"
            });
        }).RequireAuthorization();

        endpoints.MapGet("/api/documents/report/activity", async (HttpContext context, IDocumentActivityService documentActivityService, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var report = await documentActivityService.GetActivitySummaryAsync(userId.Value, cancellationToken);
            return Results.Ok(report);
        }).RequireAuthorization("Administrator");

        return endpoints;
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var userId) ? userId : null;
    }

    private static string? GetDepartment(ClaimsPrincipal user)
    {
        return user.FindFirst("department")?.Value;
    }
}