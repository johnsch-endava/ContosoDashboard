# Quickstart: Document Upload and Management

## Prerequisites

- .NET 10 SDK installed
- Repository checked out locally
- No external cloud account or antivirus engine required for the training build

## Run the application

From the repository root:

```bash
dotnet build ContosoDashboard/ContosoDashboard.csproj
dotnet run --project ContosoDashboard/ContosoDashboard.csproj --launch-profile http
```

Open `http://localhost:5000` and sign in through the mock login page.

## Cross-platform note

- On WSL/Linux development hosts, keep using the `http` launch profile unless you explicitly want to configure local certificate trust for HTTPS.
- Keep document storage paths relative and built with `Path.Combine(...)`; do not hard-code Windows or Unix separators.

## Document storage and scan worker setup

- The training default writes uploaded files to `ContosoDashboard/ContosoDashboard/AppData/uploads`.
- `DocumentScanning:Mode` should remain `OfflineValidation` for local/offline use.
- `DocumentScanning:EnableQueueDispatch` should stay `false` unless you are wiring the optional Azure Queue Storage plus Azure Functions scan pipeline.
- The optional Azure Functions scaffold is under `ContosoDashboard/DocumentScanWorker`.
- Copy `ContosoDashboard/DocumentScanWorker/local.settings.json.example` to `local.settings.json` only when running the queue-triggered worker locally.

## Reset the training environment

Delete these local artifacts if you need a clean dataset before rerunning the scenarios:

- `ContosoDashboard/ContosoDashboard/ContosoDashboard.db`
- `ContosoDashboard/ContosoDashboard/AppData/uploads/`
- `ContosoDashboard/DocumentScanWorker/local.settings.json`

Then rerun the build and application commands above.

## Manual validation scenarios

### 1. Valid upload to My Documents

1. Sign in as a user with upload rights.
2. Open the document upload surface.
3. Upload a supported file smaller than 25 MB with a title and one of the predefined categories.
4. Confirm the file appears in `My Documents` with title, category, upload date, file size, and optional project/task metadata.

### 2. Independent failure handling during multi-file upload

1. Select multiple files where at least one file is unsupported or oversized.
2. Start upload.
3. Confirm each file reports its own progress and final status.
4. Confirm valid files succeed even if one file is rejected.

### 3. Project and task context visibility

1. Upload a document while associating it to a project.
2. Confirm the document appears in the related project view for current project participants.
3. Upload or attach a document from a task context.
4. Confirm the document appears in the task surface and remains associated with the task's project.

### 4. Authorization denial

1. Sign in as a user who does not own the document and lacks project or share access.
2. Attempt to open preview, download, replace, or delete the document.
3. Confirm access is denied and no file content is exposed.

### 5. Department share and notification

1. Sign in as the document owner.
2. Share the document with a specific user or department team.
3. Confirm recipients receive an in-app notification.
4. Sign in as a recipient and confirm the document appears in `Shared with Me`.

### 6. Replacement and delete behavior

1. Replace an existing document file.
2. Confirm only the latest file is available through normal preview/download flows.
3. Confirm an audit/reporting view records the replacement event.
4. Delete the document and confirm it disappears from normal document views.

### 7. Dashboard integration

1. Upload several documents as the signed-in user.
2. Return to the dashboard.
3. Confirm the home page shows the five most recent uploaded documents and updated document summary counts.

## Expected implementation checkpoints

- Upload validation rejects unsupported extensions, MIME mismatches, and files larger than 25 MB.
- Document binaries are stored under `AppData/uploads` and are not directly browsable under `wwwroot`.
- Preview and download use authenticated endpoints that enforce the same request-time authorization rules as the UI.
- Activity logging captures upload, preview, download, share, replacement, and delete events.