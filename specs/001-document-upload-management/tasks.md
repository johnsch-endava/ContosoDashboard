# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Use focused build validation with `dotnet build ContosoDashboard/ContosoDashboard.csproj` plus the manual validation flows documented in `specs/001-document-upload-management/quickstart.md`. The specification does not request TDD or a new automated test harness, so no separate automated test tasks are included.

**Constitution Alignment**: Tasks preserve offline/local defaults, include service-level authorization and secure file-serving work for user-scoped behavior, and update operator guidance when runtime or deployment behavior changes.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment where practical.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish configuration and project scaffolding for local storage and the optional async scan worker.

- [ ] T001 Add document storage, screening, and queue-scan configuration sections in ContosoDashboard/ContosoDashboard/appsettings.json and ContosoDashboard/ContosoDashboard/appsettings.Development.json
- [ ] T002 Create the optional scan-worker project scaffold in ContosoDashboard/DocumentScanWorker/DocumentScanWorker.csproj, ContosoDashboard/DocumentScanWorker/host.json, and ContosoDashboard/DocumentScanWorker/local.settings.json.example
- [ ] T003 [P] Add setup guidance for document storage paths and scan-worker prerequisites in ContosoDashboard/README.md and ContosoDashboard/specs/001-document-upload-management/quickstart.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before any user story can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 Create document domain models and scan-status enums in ContosoDashboard/ContosoDashboard/Models/Document.cs, ContosoDashboard/ContosoDashboard/Models/DocumentTag.cs, ContosoDashboard/ContosoDashboard/Models/DocumentShare.cs, and ContosoDashboard/ContosoDashboard/Models/DocumentActivityRecord.cs
- [ ] T005 [P] Extend entity navigation properties for documents in ContosoDashboard/ContosoDashboard/Models/User.cs, ContosoDashboard/ContosoDashboard/Models/Project.cs, and ContosoDashboard/ContosoDashboard/Models/TaskItem.cs
- [ ] T006 Update document persistence, indexes, and relational constraints in ContosoDashboard/ContosoDashboard/Data/ApplicationDbContext.cs
- [ ] T007 [P] Add file storage abstractions and the local filesystem implementation in ContosoDashboard/ContosoDashboard/Services/IFileStorageService.cs and ContosoDashboard/ContosoDashboard/Services/LocalFileStorageService.cs
- [ ] T008 [P] Add screening abstractions and the offline validation implementation in ContosoDashboard/ContosoDashboard/Services/IFileScreeningService.cs and ContosoDashboard/ContosoDashboard/Services/ValidationFileScreeningService.cs
- [ ] T009 [P] Add async scan dispatch abstractions and the queue message contract in ContosoDashboard/ContosoDashboard/Services/IFileScanDispatchService.cs, ContosoDashboard/ContosoDashboard/Services/NoOpFileScanDispatchService.cs, and ContosoDashboard/ContosoDashboard/Models/DocumentScanRequest.cs
- [ ] T010 [P] Add document request and response DTOs in ContosoDashboard/ContosoDashboard/Models/DocumentRequests.cs and ContosoDashboard/ContosoDashboard/Models/DocumentResponses.cs
- [ ] T011 Update authentication claims to emit department identity for share authorization in ContosoDashboard/ContosoDashboard/Pages/Login.cshtml.cs
- [ ] T012 Register document services, storage services, screening services, scan dispatch, and endpoint mapping in ContosoDashboard/ContosoDashboard/Program.cs and ContosoDashboard/ContosoDashboard/Endpoints/DocumentEndpoints.cs

**Checkpoint**: Foundation ready. Document entities, storage/screening seams, claims, and endpoint registration are in place.

---

## Phase 3: User Story 1 - Upload and Organize Work Documents (Priority: P1) 🎯 MVP

**Goal**: Let authenticated users upload supported documents with metadata and immediately find them in My Documents and project document views.

**Independent Test**: Sign in as an authorized user, upload one or more supported files with valid metadata, verify success/failure messages per file, and confirm the documents appear in My Documents and the related project view when a project is assigned.

### Implementation for User Story 1

- [ ] T013 [P] [US1] Implement document upload, list, filter, sort, and search methods in ContosoDashboard/ContosoDashboard/Services/IDocumentService.cs and ContosoDashboard/ContosoDashboard/Services/DocumentService.cs
- [ ] T014 [US1] Implement `/api/documents` list/upload and `/api/projects/{projectId}/documents` handlers in ContosoDashboard/ContosoDashboard/Endpoints/DocumentEndpoints.cs
- [ ] T015 [P] [US1] Build reusable upload and document table components in ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentUploadPanel.razor and ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentTable.razor
- [ ] T016 [US1] Create the My Documents page with per-file upload feedback, sorting, filtering, and search in ContosoDashboard/ContosoDashboard/Pages/Documents.razor
- [ ] T017 [US1] Add project document browsing and project-context upload entry points in ContosoDashboard/ContosoDashboard/Pages/ProjectDetails.razor
- [ ] T018 [P] [US1] Add document navigation and shared imports wiring in ContosoDashboard/ContosoDashboard/Shared/NavMenu.razor and ContosoDashboard/ContosoDashboard/Pages/_Imports.razor
- [ ] T019 [US1] Enforce request-time project membership and owner-scoped authorization for upload and listing flows in ContosoDashboard/ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/ContosoDashboard/Endpoints/DocumentEndpoints.cs
- [ ] T020 [US1] Record the User Story 1 validation flow in ContosoDashboard/specs/001-document-upload-management/quickstart.md

**Checkpoint**: User Story 1 delivers upload plus organized browsing in My Documents and project views.

---

## Phase 4: User Story 2 - Access, Share, and Maintain Documents (Priority: P2)

**Goal**: Let authorized users preview, download, edit, replace, delete, and share documents while recipients can find shared items and receive notifications.

**Independent Test**: Upload a document, edit its metadata, preview or download it, share it with a valid recipient, verify it appears in Shared with Me, and confirm unauthorized users cannot preview, download, replace, share, or delete the document.

### Implementation for User Story 2

- [ ] T021 [P] [US2] Implement document activity logging services and audit helpers in ContosoDashboard/ContosoDashboard/Services/IDocumentActivityService.cs and ContosoDashboard/ContosoDashboard/Services/DocumentActivityService.cs
- [ ] T022 [US2] Extend document access methods for metadata updates, preview, download, replace, delete, and share actions in ContosoDashboard/ContosoDashboard/Services/IDocumentService.cs and ContosoDashboard/ContosoDashboard/Services/DocumentService.cs
- [ ] T023 [US2] Implement detail, preview, download, patch, delete, replace, and share handlers in ContosoDashboard/ContosoDashboard/Endpoints/DocumentEndpoints.cs
- [ ] T024 [P] [US2] Add preview, download, edit, replace, and delete controls in ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentActionBar.razor and ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentEditForm.razor
- [ ] T025 [P] [US2] Add sharing UI and the Shared with Me experience in ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentShareDialog.razor and ContosoDashboard/ContosoDashboard/Pages/Documents.razor
- [ ] T026 [US2] Integrate department-share resolution and in-app share notifications in ContosoDashboard/ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/ContosoDashboard/Services/NotificationService.cs
- [ ] T027 [US2] Enforce replacement/delete authorization and active-file-only replacement behavior in ContosoDashboard/ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/ContosoDashboard/Endpoints/DocumentEndpoints.cs
- [ ] T028 [US2] Record the User Story 2 validation flow in ContosoDashboard/specs/001-document-upload-management/quickstart.md

**Checkpoint**: User Stories 1 and 2 are independently functional, including secure access and collaboration flows.

---

## Phase 5: User Story 3 - See Documents in Context and Oversight Views (Priority: P3)

**Goal**: Surface documents in task, dashboard, and administrative oversight workflows, including recent activity and reporting views.

**Independent Test**: Attach or upload a document from a task context, confirm task and dashboard views reflect the document activity, and verify that administrators can review document activity and scan status without using the upload flow directly.

### Implementation for User Story 3

- [ ] T029 [P] [US3] Add task-context document query methods and task association helpers in ContosoDashboard/ContosoDashboard/Services/IDocumentService.cs and ContosoDashboard/ContosoDashboard/Services/DocumentService.cs
- [ ] T030 [US3] Implement `/api/tasks/{taskId}/documents` and reporting query handlers in ContosoDashboard/ContosoDashboard/Endpoints/DocumentEndpoints.cs
- [ ] T031 [US3] Add related document surfaces and task-context upload flow in ContosoDashboard/ContosoDashboard/Pages/Tasks.razor
- [ ] T032 [US3] Add dashboard recent documents and document summary counts in ContosoDashboard/ContosoDashboard/Services/DashboardService.cs and ContosoDashboard/ContosoDashboard/Pages/Index.razor
- [ ] T033 [US3] Add administrative reporting and audit UI for document activity and scan status in ContosoDashboard/ContosoDashboard/Pages/DocumentsAdmin.razor and ContosoDashboard/ContosoDashboard/Shared/NavMenu.razor
- [ ] T034 [US3] Notify project members about new accessible project documents and surface scan/quarantine state where applicable in ContosoDashboard/ContosoDashboard/Services/DocumentService.cs, ContosoDashboard/ContosoDashboard/Services/NotificationService.cs, ContosoDashboard/ContosoDashboard/Pages/Documents.razor, and ContosoDashboard/ContosoDashboard/Pages/ProjectDetails.razor
- [ ] T035 [US3] Record the User Story 3 validation flow in ContosoDashboard/specs/001-document-upload-management/quickstart.md

**Checkpoint**: All three user stories are functional, including contextual document visibility and oversight views.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Complete optional production scanning, harden cross-story behavior, and finish operator guidance.

- [ ] T036 [P] Implement Azure Queue Storage dispatch for post-upload scan jobs in ContosoDashboard/ContosoDashboard/Services/AzureQueueFileScanDispatchService.cs and ContosoDashboard/ContosoDashboard/appsettings.json
- [ ] T037 [P] Create the Azure Functions queue-triggered virus-scan worker in ContosoDashboard/DocumentScanWorker/Functions/ScanDocumentFunction.cs, ContosoDashboard/DocumentScanWorker/Services/ScanExecutionService.cs, and ContosoDashboard/DocumentScanWorker/host.json
- [ ] T038 [P] Handle scan-result persistence, quarantine transitions, and administrative failure notifications in ContosoDashboard/ContosoDashboard/Services/DocumentScanResultService.cs, ContosoDashboard/ContosoDashboard/Services/DocumentActivityService.cs, and ContosoDashboard/ContosoDashboard/Services/NotificationService.cs
- [ ] T039 Update operator guidance for offline mode versus queue-backed scan mode in ContosoDashboard/specs/001-document-upload-management/quickstart.md and ContosoDashboard/README.md
- [ ] T040 Harden logging/error handling and capture final validation notes in ContosoDashboard/ContosoDashboard/Services/DocumentService.cs, ContosoDashboard/ContosoDashboard/Endpoints/DocumentEndpoints.cs, ContosoDashboard/DocumentScanWorker/Functions/ScanDocumentFunction.cs, and ContosoDashboard/specs/001-document-upload-management/plan.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup**: No dependencies; start immediately.
- **Phase 2: Foundational**: Depends on Phase 1 and blocks all user stories.
- **Phase 3: US1**: Depends on Phase 2; delivers the MVP document workflow.
- **Phase 4: US2**: Depends on US1 because secure access and sharing extend the uploaded-document surfaces created in the MVP.
- **Phase 5: US3**: Depends on US1 for core document flows and on US2 for audit/share behavior reused in oversight views.
- **Phase 6: Polish**: Depends on the user stories that are in scope for the release.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational and has no dependency on later stories.
- **US2 (P2)**: Builds on US1 document creation/listing but must remain independently testable once a document exists.
- **US3 (P3)**: Builds on the document domain plus audit/reporting capabilities and must remain independently testable through task, dashboard, and admin flows.

### Parallel Opportunities

- **Setup**: T003 can run in parallel with T001-T002.
- **Foundational**: T005, T007, T008, T009, and T010 can run in parallel once T004 has established the document domain direction.
- **US1**: T015 and T018 can run in parallel after T013-T014 define the upload/list contract.
- **US2**: T021, T024, and T025 can run in parallel once T022 defines the action set.
- **US3**: T031 and T032 can run in parallel once T029-T030 establish the service and endpoint contracts.
- **Polish**: T036, T037, and T038 can run in parallel because they target separate worker, dispatch, and result-handling files.

---

## Parallel Example: User Story 1

```text
Task: "T015 [US1] Build reusable upload and document table components in ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentUploadPanel.razor and ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentTable.razor"
Task: "T018 [US1] Add document navigation and shared imports wiring in ContosoDashboard/ContosoDashboard/Shared/NavMenu.razor and ContosoDashboard/ContosoDashboard/Pages/_Imports.razor"
```

## Parallel Example: User Story 2

```text
Task: "T021 [US2] Implement document activity logging services and audit helpers in ContosoDashboard/ContosoDashboard/Services/IDocumentActivityService.cs and ContosoDashboard/ContosoDashboard/Services/DocumentActivityService.cs"
Task: "T024 [US2] Add preview, download, edit, replace, and delete controls in ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentActionBar.razor and ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentEditForm.razor"
Task: "T025 [US2] Add sharing UI and the Shared with Me experience in ContosoDashboard/ContosoDashboard/Shared/Documents/DocumentShareDialog.razor and ContosoDashboard/ContosoDashboard/Pages/Documents.razor"
```

## Parallel Example: User Story 3

```text
Task: "T031 [US3] Add related document surfaces and task-context upload flow in ContosoDashboard/ContosoDashboard/Pages/Tasks.razor"
Task: "T032 [US3] Add dashboard recent documents and document summary counts in ContosoDashboard/ContosoDashboard/Services/DashboardService.cs and ContosoDashboard/ContosoDashboard/Pages/Index.razor"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Validate upload, list, filter, and project-view behavior with the quickstart scenarios.
5. Demo or ship the MVP if only the upload-and-organize workflow is needed.

### Incremental Delivery

1. Deliver Setup + Foundational to establish the document platform.
2. Deliver US1 for upload and browsing.
3. Deliver US2 for secure access, maintenance, and sharing.
4. Deliver US3 for contextual visibility and administrative oversight.
5. Finish with the optional queue-backed scan worker and cross-cutting hardening.

### Parallel Team Strategy

1. Complete Setup and Foundational together.
2. After US1 starts, assign UI-heavy tasks and service-heavy tasks in parallel where marked `[P]`.
3. Use Phase 6 to split web-app queue dispatch, Azure Function worker, and scan-result handling across separate contributors.

---

## Notes

- `[P]` tasks touch separate files and can be performed in parallel after their dependencies are satisfied.
- Story labels map every implementation task to a specific user story for traceability.
- Manual validation remains the expected verification path for this feature until a dedicated automated app test harness exists.
- The offline/local training flow remains the default release path; Azure Queue Storage plus Azure Functions work is optional production-oriented hardening.