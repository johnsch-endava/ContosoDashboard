# Data Model: Document Upload and Management

## Entity: Document

**Purpose**: Represents the current document record, its searchable business metadata, and the storage pointer to the active binary.

| Field | Type | Required | Notes |
|------|------|----------|-------|
| `DocumentId` | `int` | Yes | Primary key; integer for consistency with existing models |
| `Title` | `string` | Yes | Max 255; required at upload |
| `Description` | `string?` | No | Max 2000 |
| `Category` | `string` | Yes | One of: `Project Documents`, `Team Resources`, `Personal Files`, `Reports`, `Presentations`, `Other` |
| `OriginalFileName` | `string` | Yes | User-facing file name for display/download |
| `StoredFileName` | `string` | Yes | GUID-based file name used on disk |
| `RelativeStoragePath` | `string` | Yes | Relative path under `AppData/uploads`; never web-accessible directly |
| `MimeType` | `string` | Yes | Max 255 to accommodate Office MIME types |
| `FileExtension` | `string` | Yes | Normalized lower-case extension for validation and preview rules |
| `FileSizeBytes` | `long` | Yes | Must be `<= 26214400` |
| `UploadedByUserId` | `int` | Yes | FK to `User` |
| `ProjectId` | `int?` | No | FK to `Project`; required for project-scoped uploads |
| `TaskId` | `int?` | No | FK to `TaskItem`; if set, the task's project must match `ProjectId` |
| `CreatedUtc` | `DateTime` | Yes | Upload timestamp |
| `UpdatedUtc` | `DateTime` | Yes | Metadata or replacement update timestamp |
| `DeletedUtc` | `DateTime?` | No | Tombstone for removed documents; excluded from normal queries |
| `LastScreenedUtc` | `DateTime` | Yes | Timestamp of first-release validation screening |
| `ScreeningOutcome` | `string` | Yes | `Passed` or rejected result recorded before availability |

**Relationships**:

- Many-to-one with `User` through `UploadedByUserId`
- Many-to-one with `Project` through `ProjectId`
- Optional many-to-one with `TaskItem` through `TaskId`
- One-to-many with `DocumentTag`
- One-to-many with `DocumentShare`
- One-to-many with `DocumentActivityRecord`

**Validation Rules**:

- `Title` and `Category` are required before upload begins.
- `Category` must be one of the predefined first-release values.
- `FileSizeBytes` must be less than or equal to 25 MB.
- Extension and MIME type must pass the offline screening policy before the document becomes available.
- If `TaskId` is supplied, `ProjectId` must be populated and match the task's project.
- If `DeletedUtc` is set, the document must not appear in `My Documents`, `Shared with Me`, project documents, task documents, preview, or download results.

## Entity: DocumentTag

**Purpose**: Stores searchable custom tags as first-class values rather than serialized text.

| Field | Type | Required | Notes |
|------|------|----------|-------|
| `DocumentTagId` | `int` | Yes | Primary key |
| `DocumentId` | `int` | Yes | FK to `Document` |
| `TagValue` | `string` | Yes | Max 50; normalized for case-insensitive search |
| `CreatedUtc` | `DateTime` | Yes | Audit helper |

**Relationships**:

- Many-to-one with `Document`

**Validation Rules**:

- Duplicate tags are not allowed per document.
- Empty or whitespace-only tags are rejected.

## Entity: DocumentShare

**Purpose**: Grants document visibility outside ownership or project membership.

| Field | Type | Required | Notes |
|------|------|----------|-------|
| `DocumentShareId` | `int` | Yes | Primary key |
| `DocumentId` | `int` | Yes | FK to `Document` |
| `SharedByUserId` | `int` | Yes | FK to `User` |
| `SharedWithUserId` | `int?` | No | FK to `User` when share target is a person |
| `SharedWithDepartment` | `string?` | No | Department/team identity when share target is a department team |
| `AccessLevel` | `string` | Yes | First release uses `Read`; field preserves future extensibility |
| `CreatedUtc` | `DateTime` | Yes | Share timestamp |

**Relationships**:

- Many-to-one with `Document`
- Many-to-one with `User` through `SharedByUserId`
- Optional many-to-one with `User` through `SharedWithUserId`

**Validation Rules**:

- Exactly one target must be supplied: `SharedWithUserId` or `SharedWithDepartment`.
- Department-based shares are resolved against the recipient's current department identity at request time.
- The owner may not create duplicate active shares for the same target.
- Shares do not grant delete or replacement rights in the first release.

## Entity: DocumentActivityRecord

**Purpose**: Provides the audit trail and reporting source for document events.

| Field | Type | Required | Notes |
|------|------|----------|-------|
| `DocumentActivityRecordId` | `int` | Yes | Primary key |
| `DocumentId` | `int` | Yes | FK to `Document` |
| `ActorUserId` | `int` | Yes | FK to `User` |
| `ActivityType` | `string` | Yes | `Upload`, `Preview`, `Download`, `Replacement`, `Delete`, `Share` |
| `OccurredUtc` | `DateTime` | Yes | Event timestamp |
| `DetailsJson` | `string?` | No | Optional structured details such as share target or file-change metadata |

**Relationships**:

- Many-to-one with `Document`
- Many-to-one with `User`

**Validation Rules**:

- An activity record is created for every upload, preview, download, replacement, delete, and share action.
- Delete and replacement records must be written before the old binary is permanently discarded.

## Derived Views and Query Shapes

- **My Documents**: Documents where `UploadedByUserId == currentUserId` and `DeletedUtc == null`
- **Shared with Me**: Documents with a matching `DocumentShare` for `currentUserId` or the user's current department and `DeletedUtc == null`
- **Project Documents**: Documents with `ProjectId == requestedProjectId`, `DeletedUtc == null`, and current project access
- **Task Documents**: Documents with `TaskId == requestedTaskId`, `DeletedUtc == null`, and task access
- **Dashboard Recent Documents**: Five most recent documents uploaded by the current user where `DeletedUtc == null`

## State Transitions

### Document lifecycle

```text
PendingUpload
  -> Validating
  -> Available
  -> Deleted

Available
  -> Replacing -> Available
  -> Deleted
```

**Notes**:

- `PendingUpload` and `Validating` are workflow states owned by the service/UI flow; the record is only queryable as `Available` after storage and screening succeed.
- Replacement does not create an end-user-visible version history in the first release; it swaps the active binary and logs a `Replacement` activity.
- `Deleted` removes the binary from storage and excludes the record from user-facing queries; no restore flow is planned.