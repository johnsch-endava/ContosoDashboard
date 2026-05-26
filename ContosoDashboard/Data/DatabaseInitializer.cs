using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Data;

internal static class DatabaseInitializer
{
    private static readonly string[] LegacyDocumentSchemaStatements =
    [
        """
        CREATE TABLE IF NOT EXISTS "Documents" (
            "DocumentId" INTEGER NOT NULL CONSTRAINT "PK_Documents" PRIMARY KEY AUTOINCREMENT,
            "Title" TEXT NOT NULL,
            "Description" TEXT NULL,
            "Category" TEXT NOT NULL,
            "OriginalFileName" TEXT NOT NULL,
            "StoredFileName" TEXT NOT NULL,
            "RelativeStoragePath" TEXT NOT NULL,
            "MimeType" TEXT NOT NULL,
            "FileExtension" TEXT NOT NULL,
            "FileSizeBytes" INTEGER NOT NULL,
            "UploadedByUserId" INTEGER NOT NULL,
            "ProjectId" INTEGER NULL,
            "TaskId" INTEGER NULL,
            "CreatedUtc" TEXT NOT NULL,
            "UpdatedUtc" TEXT NOT NULL,
            "DeletedUtc" TEXT NULL,
            "LastScreenedUtc" TEXT NULL,
            "ScanStatus" INTEGER NOT NULL,
            "ScreeningOutcome" TEXT NOT NULL,
            CONSTRAINT "FK_Documents_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("ProjectId") ON DELETE SET NULL,
            CONSTRAINT "FK_Documents_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("TaskId") ON DELETE SET NULL,
            CONSTRAINT "FK_Documents_Users_UploadedByUserId" FOREIGN KEY ("UploadedByUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS "DocumentActivityRecords" (
            "DocumentActivityRecordId" INTEGER NOT NULL CONSTRAINT "PK_DocumentActivityRecords" PRIMARY KEY AUTOINCREMENT,
            "DocumentId" INTEGER NOT NULL,
            "ActorUserId" INTEGER NOT NULL,
            "ActivityType" INTEGER NOT NULL,
            "OccurredUtc" TEXT NOT NULL,
            "DetailsJson" TEXT NULL,
            CONSTRAINT "FK_DocumentActivityRecords_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("DocumentId") ON DELETE CASCADE,
            CONSTRAINT "FK_DocumentActivityRecords_Users_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS "DocumentShares" (
            "DocumentShareId" INTEGER NOT NULL CONSTRAINT "PK_DocumentShares" PRIMARY KEY AUTOINCREMENT,
            "DocumentId" INTEGER NOT NULL,
            "SharedByUserId" INTEGER NOT NULL,
            "SharedWithUserId" INTEGER NULL,
            "SharedWithDepartment" TEXT NULL,
            "AccessLevel" TEXT NOT NULL,
            "CreatedUtc" TEXT NOT NULL,
            CONSTRAINT "FK_DocumentShares_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("DocumentId") ON DELETE CASCADE,
            CONSTRAINT "FK_DocumentShares_Users_SharedByUserId" FOREIGN KEY ("SharedByUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT,
            CONSTRAINT "FK_DocumentShares_Users_SharedWithUserId" FOREIGN KEY ("SharedWithUserId") REFERENCES "Users" ("UserId")
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS "DocumentTags" (
            "DocumentTagId" INTEGER NOT NULL CONSTRAINT "PK_DocumentTags" PRIMARY KEY AUTOINCREMENT,
            "DocumentId" INTEGER NOT NULL,
            "TagValue" TEXT NOT NULL,
            "CreatedUtc" TEXT NOT NULL,
            CONSTRAINT "FK_DocumentTags_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("DocumentId") ON DELETE CASCADE
        );
        """,
        """CREATE INDEX IF NOT EXISTS "IX_Documents_Category" ON "Documents" ("Category");""",
        """CREATE INDEX IF NOT EXISTS "IX_Documents_ProjectId_DeletedUtc" ON "Documents" ("ProjectId", "DeletedUtc");""",
        """CREATE INDEX IF NOT EXISTS "IX_Documents_TaskId" ON "Documents" ("TaskId");""",
        """CREATE INDEX IF NOT EXISTS "IX_Documents_UploadedByUserId_CreatedUtc" ON "Documents" ("UploadedByUserId", "CreatedUtc");""",
        """CREATE INDEX IF NOT EXISTS "IX_DocumentActivityRecords_ActorUserId" ON "DocumentActivityRecords" ("ActorUserId");""",
        """CREATE INDEX IF NOT EXISTS "IX_DocumentActivityRecords_DocumentId_OccurredUtc" ON "DocumentActivityRecords" ("DocumentId", "OccurredUtc");""",
        """CREATE INDEX IF NOT EXISTS "IX_DocumentShares_DocumentId_SharedWithUserId_SharedWithDepartment" ON "DocumentShares" ("DocumentId", "SharedWithUserId", "SharedWithDepartment");""",
        """CREATE INDEX IF NOT EXISTS "IX_DocumentShares_SharedByUserId" ON "DocumentShares" ("SharedByUserId");""",
        """CREATE INDEX IF NOT EXISTS "IX_DocumentShares_SharedWithUserId" ON "DocumentShares" ("SharedWithUserId");""",
        """CREATE UNIQUE INDEX IF NOT EXISTS "IX_DocumentTags_DocumentId_TagValue" ON "DocumentTags" ("DocumentId", "TagValue");"""
    ];

    public static async Task InitializeAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        if (!context.Database.IsSqlite())
        {
            return;
        }

        await EnsureLegacySqliteDocumentSchemaAsync(context, logger, cancellationToken);
    }

    private static async Task EnsureLegacySqliteDocumentSchemaAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;

        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var documentsTableExists = await TableExistsAsync(connection, "Documents", cancellationToken);

            foreach (var statement in LegacyDocumentSchemaStatements)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = statement;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            if (!documentsTableExists)
            {
                logger.LogInformation("Applied legacy SQLite document schema patch.");
            }
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = $tableName;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is long count && count > 0;
    }
}