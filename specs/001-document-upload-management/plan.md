# Implementation Plan: Document Upload and Management

**Branch**: `[001-document-upload-management]` | **Date**: 2026-05-25 | **Spec**: `/specs/001-document-upload-management/spec.md`
**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

## Summary

Add document upload and management to the existing Blazor Server training app by introducing a document domain model, secured local file storage outside `wwwroot`, service-level authorization for every document read/write action, and UI integration across documents, project, task, dashboard, and notification surfaces. The first release stays offline-first by using SQLite plus local filesystem storage, validation-based screening behind a pluggable abstraction, and explicit HTTP endpoints for preview and download. The design also preserves a production migration path for asynchronous malware scanning through Azure Queue Storage and an Azure Functions queue-triggered worker after upload.

## Technical Context

**Language/Version**: C# with .NET 10 / ASP.NET Core 10 Blazor Server  
**Primary Dependencies**: Blazor Server, Entity Framework Core 10, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.SqlServer, cookie authentication, Bootstrap 5.3, optional Azure Functions plus Azure Queue Storage for production-grade async file scanning  
**Storage**: SQLite in development, optional SQL Server outside development, local filesystem storage under `ContosoDashboard/AppData/uploads` for document binaries, optional Azure Queue Storage messages for async scan requests in cloud-backed deployments  
**Testing**: `dotnet build ContosoDashboard/ContosoDashboard.csproj` plus focused manual browser validation; no dedicated automated test project exists yet for the app  
**Target Platform**: Blazor Server web app for local Windows and WSL/Linux development hosts  
**Project Type**: Single-project web application  
**Performance Goals**: Uploads up to 25 MB complete within 30 seconds for 95% of attempts; list and search results for up to 500 accessible documents render within 2 seconds for 95% of requests; previewable documents load within 3 seconds  
**Constraints**: Offline-capable by default, local storage outside `wwwroot`, 25 MB per file maximum, request-time authorization checks, no required cloud or antivirus dependency in the training build, cross-platform path handling, preserve mock-auth training flow, and keep any Azure-based scan worker as an optional production path behind abstractions  
**Scale/Scope**: One Blazor Server app, document access scoped to current user/project/department rules, lists and search optimized for up to 500 accessible documents per user in the training environment

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Gate Review

- **PASS**: Spec-first scope is defined with a concrete feature spec and clarified decisions for team sharing, screening, request-time access, replacement behavior, and category vocabulary.
- **PASS**: Training-safe runtime boundaries remain intact because the design keeps SQLite as the development datastore, uses local filesystem storage, and places future cloud or scanner integrations behind abstractions.
- **PASS**: Security obligations are explicit: protected UI surfaces, secure file-serving endpoints, service-level authorization, request-time project membership checks, and audit logging for uploads/downloads/deletes/shares/replacements.
- **PASS**: The work can be split into independently verifiable slices around upload, browsing/search, sharing/access control, and integration surfaces with focused build-plus-manual checks.
- **PASS**: Cross-platform local operations are covered by keeping runtime storage under application-controlled paths and preserving the documented HTTP launch profile behavior for WSL/Linux development.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── document-management-api.yaml
└── tasks.md                # Created later by /speckit.tasks
```

### Source Code (repository root)

```text
ContosoDashboard/
├── ContosoDashboard/
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Models/
│   │   ├── Announcement.cs
│   │   ├── Notification.cs
│   │   ├── Project.cs
│   │   ├── ProjectMember.cs
│   │   ├── TaskComment.cs
│   │   ├── TaskItem.cs
│   │   ├── User.cs
│   │   └── Document*.cs            # Planned feature additions
│   ├── Pages/
│   │   ├── Index.razor
│   │   ├── Notifications.razor
│   │   ├── Profile.razor
│   │   ├── ProjectDetails.razor
│   │   ├── Projects.razor
│   │   ├── Tasks.razor
│   │   ├── Team.razor
│   │   └── Documents*.razor        # Planned feature additions
│   ├── Services/
│   │   ├── CustomAuthenticationStateProvider.cs
│   │   ├── DashboardService.cs
│   │   ├── NotificationService.cs
│   │   ├── ProjectService.cs
│   │   ├── TaskService.cs
│   │   ├── UserService.cs
│   │   └── Document*.cs            # Planned feature additions
│   ├── Shared/
│   │   └── NavMenu.razor
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── ContosoDashboard.csproj
├── specs/
│   └── 001-document-upload-management/
└── README.md

Runtime-only additions:
ContosoDashboard/ContosoDashboard/AppData/uploads/

Optional production additions:
Azure Functions scan worker + Azure Queue Storage for async scan requests
```

**Structure Decision**: Extend the existing single Blazor Server project in place. Add document entities to `Models`, persistence and indexes to `Data/ApplicationDbContext.cs`, business logic and authorization to new `Services` implementations, navigation plus user-facing flows under `Pages` and `Shared/NavMenu.razor`, and secure streaming endpoints in the web project for preview/download because document binaries stay outside `wwwroot`.

## Phase 0: Research

Research resolved the main implementation choices that were still open after specification:

- Use local filesystem storage under `AppData/uploads` with a dedicated `IFileStorageService` abstraction.
- Use validation-based screening in the offline build through an `IFileScreeningService` seam, while reserving a production path that enqueues post-upload scan work to Azure Queue Storage for Azure Functions processing.
- Evaluate project-derived access at request time, combine it with explicit user/department shares, and keep authorization in service methods and file-serving endpoints.
- Treat each selected file as an independently validated upload operation so the UI can support multi-file upload without coupling metadata, progress, and failure handling into one fragile batch transaction.
- Keep only the latest active binary after replacement while logging an audit record, and remove deleted files from normal access paths without adding a restore workflow.

**Output**: `/specs/001-document-upload-management/research.md`

## Phase 1: Design & Contracts

### Data Model

The first-release design introduces four main persistence concepts:

- `Document` for metadata, file-location bookkeeping, ownership, optional project/task relationships, and deletion tombstones.
- `DocumentTag` for searchable custom tags without forcing CSV parsing into queries.
- `DocumentShare` for explicit user or department-based access grants.
- `DocumentActivityRecord` for audit and reporting events including upload, preview, download, replacement, delete, and share.

### Service and Endpoint Design

The feature will add these internal seams and interfaces:

- `IDocumentService` to own authorization, upload workflow, metadata updates, replace/delete/share behavior, search, filtering, and dashboard/task/project queries.
- `IFileStorageService` to isolate local storage from future Azure Blob storage migration.
- `IFileScreeningService` to isolate first-release validation screening from future scanner integration.
- `IFileScanDispatchService` to publish asynchronous scan requests after upload when the deployment enables queue-backed scanning.
- `IDocumentActivityService` or equivalent helper to centralize audit logging and reporting queries.
- Authenticated HTTP endpoints for preview/download/upload/replace operations because binaries are stored outside `wwwroot`.

### Background Scan Job Design

For the offline training build, the document pipeline stops at validation-based screening and immediate availability. For cloud-backed deployments, the same upload workflow should optionally publish a scan request message after the binary is stored and the document record is created.

- The web app writes a compact scan message containing document ID, storage path/blob name, MIME type, uploader ID, and correlation ID.
- Azure Queue Storage carries the scan request.
- An Azure Functions queue-triggered job dequeues the message, downloads or opens the stored file, runs the integrated malware scanner, and updates the document scan status plus activity log.
- If the scan fails or detects a threat, the worker marks the document unavailable or quarantined and emits an administrative notification.
- This queue-dispatch step must stay optional so local/offline training runs do not require Azure resources.

Recommended ownership split:

- Blazor app: upload, metadata persistence, initial validation, queue dispatch, user-facing pending-scan state
- Azure Function: heavy scan execution, retries, poison-message handling, final scan-status update, and failure notifications

### UI Integration Targets

- Add a top-level documents surface for `My Documents` and `Shared with Me`.
- Extend `ProjectDetails.razor` with project-document visibility and project-context upload.
- Extend `Tasks.razor` or task detail interactions with related-document visibility and upload-from-task behavior.
- Extend `Index.razor` and `DashboardService` with recent document activity and document counts.
- Reuse `NotificationService` patterns for share notifications and project-document alerts.

### Post-Design Constitution Check

- **PASS**: The design preserves offline/local training defaults and keeps production-only concerns behind abstractions.
- **PASS**: Security-sensitive behavior is routed through service-level authorization plus secure file-serving endpoints rather than UI-only checks.
- **PASS**: The slices remain independently verifiable through upload validation, access-denial checks, sharing flows, and dashboard/task/project integration checks.
- **PASS**: Cross-platform behavior is documented through relative path storage, `Path.Combine`-based implementation expectations, and HTTP profile guidance for WSL/Linux.

**Outputs**:

- `/specs/001-document-upload-management/data-model.md`
- `/specs/001-document-upload-management/contracts/document-management-api.yaml`
- `/specs/001-document-upload-management/quickstart.md`

## Phase 2: Task Planning Approach

Task generation should stay aligned to independently verifiable slices:

1. Persistence and infrastructure: document entities, DbContext updates, storage/screening abstractions, and configuration.
2. Core business logic: upload, request-time authorization, preview/download, replace/delete, audit logging, and optional scan-queue dispatch.
3. User document surfaces: `My Documents`, `Shared with Me`, sort/filter/search, and navigation updates.
4. Context integration: project/task views, dashboard counts/recent documents, notifications, and scan-status visibility where applicable.
5. Optional cloud worker slice: Azure Queue Storage message contract, Azure Functions queue trigger, scan result handling, retries, and quarantine/failure state transitions.
6. Validation and operator guidance: focused build validation, quickstart/manual flows, and cross-platform notes.

## Complexity Tracking

No constitution exceptions are required for this plan.