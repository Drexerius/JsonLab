CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Users" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
    "UserName" TEXT NOT NULL,
    "NormalizedName" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL
);

CREATE TABLE "Folders" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Folders" PRIMARY KEY,
    "OwnerId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "NormalizedName" TEXT NOT NULL,
    CONSTRAINT "FK_Folders_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Documents" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Documents" PRIMARY KEY,
    "OwnerId" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Content" TEXT NOT NULL,
    "FolderId" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL,
    "Version" INTEGER NOT NULL,
    CONSTRAINT "FK_Documents_Folders_FolderId" FOREIGN KEY ("FolderId") REFERENCES "Folders" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Documents_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "DocumentTag" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DocumentTag" PRIMARY KEY,
    "DocumentId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    CONSTRAINT "FK_DocumentTag_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Documents_FolderId" ON "Documents" ("FolderId");

CREATE INDEX "IX_Documents_OwnerId" ON "Documents" ("OwnerId");

CREATE UNIQUE INDEX "IX_DocumentTag_DocumentId_Name" ON "DocumentTag" ("DocumentId", "Name");

CREATE UNIQUE INDEX "IX_Folders_OwnerId_NormalizedName" ON "Folders" ("OwnerId", "NormalizedName");

CREATE UNIQUE INDEX "IX_Users_NormalizedName" ON "Users" ("NormalizedName");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261020160159_InitialWorkspace', '10.0.13');

COMMIT;

BEGIN TRANSACTION;
CREATE TABLE "ef_temp_Documents" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Documents" PRIMARY KEY,
    "Content" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "FolderId" TEXT NULL,
    "OwnerId" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Documents_Folders_FolderId" FOREIGN KEY ("FolderId") REFERENCES "Folders" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Documents_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

INSERT INTO "ef_temp_Documents" ("Id", "Content", "CreatedAtUtc", "FolderId", "OwnerId", "Title", "UpdatedAtUtc")
SELECT "Id", "Content", "CreatedAtUtc", "FolderId", "OwnerId", "Title", "UpdatedAtUtc"
FROM "Documents";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;
DROP TABLE "Documents";

ALTER TABLE "ef_temp_Documents" RENAME TO "Documents";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;
CREATE INDEX "IX_Documents_FolderId" ON "Documents" ("FolderId");

CREATE INDEX "IX_Documents_OwnerId" ON "Documents" ("OwnerId");

COMMIT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261022173259_RemoveDocumentVersion', '10.0.13');

