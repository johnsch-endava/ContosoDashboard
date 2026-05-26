# Research: Document Upload and Management

## Decision 1: Local file storage remains offline-first, with an optional async cloud scanning path

- **Decision**: Store uploaded binaries under `ContosoDashboard/AppData/uploads/{userId}/{projectId-or-personal}/{guid}.{ext}` outside `wwwroot`, serve them only through authenticated endpoints, and route first-release screening through an `IFileScreeningService` that performs extension, MIME, and size validation. Preserve an optional production path where the web app publishes a post-upload scan request to Azure Queue Storage and an Azure Functions queue-trigger handles asynchronous malware scanning.
- **Rationale**: This matches the training-safe constitution, keeps the feature offline-capable, prevents direct static-file access, and preserves a clean seam for future malware scanner integration. Queue-triggered Azure Functions fit the desired asynchronous scan job model without forcing the Blazor app to block on long-running scans.
- **Alternatives considered**: Store files under `wwwroot` for simpler serving; rejected because it weakens authorization guarantees. Require a real antivirus engine in development; rejected because it breaks the offline and cross-platform training baseline. Run malware scans synchronously inside the web app; rejected because it would couple upload latency and scanner availability to the request path. Store files in Azure Blob Storage now; rejected because the current feature explicitly preserves local/offline behavior.

### Async scan worker notes

- The upload flow remains `generate path -> save file -> save metadata`; queue dispatch happens after the document record exists.
- Queue messages should contain stable identifiers rather than large payloads: document ID, storage location, checksum if available, MIME type, and correlation ID.
- The Azure Function should use built-in retries and a poison queue so bad messages do not block the whole pipeline.
- Scan completion should update persisted scan status and write an audit activity record that administrators can report on.
- Documents may need an intermediate pending-scan state in cloud deployments if availability should wait for the async result.

## Decision 2: Authorization is centralized and evaluated at request time

- **Decision**: Route document access through `IDocumentService` checks that combine document ownership, administrator privileges, project-manager rules, current project membership, and explicit `DocumentShare` grants to individual users or departments. Update the login flow to emit the user's department claim because department-based sharing depends on it.
- **Rationale**: Existing services already protect data at the service layer, and the clarified spec requires project-derived access to change immediately when membership changes. Department-based sharing cannot be implemented safely if the authenticated principal does not carry department context.
- **Alternatives considered**: Snapshot project access when a document is uploaded; rejected because it conflicts with the clarified security model. Rely on page-level `[Authorize]` attributes only; rejected because download and preview flows need service-level and endpoint-level checks. Interpret “team” as project teams; rejected by clarification.

## Decision 3: Multi-file upload is implemented as independent per-file operations

- **Decision**: Support multi-file selection in the UI, but process each selected file as its own upload command with its own validation result, progress indication, success or failure message, and metadata save path. The file-write sequence remains generate path -> save file -> save metadata.
- **Rationale**: The spec already states that files are validated and reported independently. Per-file processing simplifies the contract, avoids partially committed batch metadata, and makes failure recovery deterministic when one file in a multi-file selection fails.
- **Alternatives considered**: One atomic batch request for all files; rejected because it complicates metadata shape, error reporting, and retry semantics. Save metadata before the file write; rejected because stakeholder guidance explicitly calls out orphaned records and duplicate-path risks.

## Decision 4: Model tags and audit explicitly, but keep version history out of scope

- **Decision**: Add `Document`, `DocumentTag`, `DocumentShare`, and `DocumentActivityRecord` persistence models. Replacement keeps one current active binary and records a `Replacement` activity instead of exposing end-user version history. Deletion removes the binary from storage and the document from normal access paths without introducing a recoverable trash workflow.
- **Rationale**: Explicit share and activity records are required by the spec, and a separate tag table keeps search/filter behavior predictable. Full version history would expand the first release beyond the clarified requirement, which only demands that replacement preserves the document record and logs the event.
- **Alternatives considered**: Store tags in a single CSV column; rejected because search and editing become brittle. Add a full `DocumentVersion` history model; rejected because it adds UI and retention complexity not required for this release. Permanently hard-delete every database row immediately; rejected because audit/reporting needs a reliable event trail and delete metadata context.

## Decision 5: Validation stays build-plus-manual for this feature slice

- **Decision**: Use `dotnet build ContosoDashboard/ContosoDashboard.csproj` as the executable validation baseline and document focused manual validation scenarios in `quickstart.md` for upload, denial, sharing, replacement, dashboard, and project/task integration flows.
- **Rationale**: The repository does not currently include an application test project, but the constitution still requires independently verifiable slices. A scoped build plus reproducible manual scenarios matches the existing repo shape and keeps the plan immediately actionable.
- **Alternatives considered**: Block planning until a new automated test harness exists; rejected because the current repository has no such harness and planning needs to move forward. Skip executable validation entirely; rejected because the constitution requires at least one focused executable check.