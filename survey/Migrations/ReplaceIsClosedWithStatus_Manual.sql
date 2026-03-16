-- Run this on your database if the migration cannot be applied (e.g. app is running).
-- Replaces IsClosed with Status: 1 = active, -1 = closed.

-- MySQL:
ALTER TABLE `Surveys` ADD COLUMN `Status` int NOT NULL DEFAULT 1;
UPDATE `Surveys` SET `Status` = IF(`IsClosed` = 1, -1, 1);
ALTER TABLE `Surveys` DROP COLUMN `IsClosed`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260316120000_ReplaceIsClosedWithStatus', '8.0.11');

-- SQL Server (run only one of the two blocks):
-- ALTER TABLE [Surveys] ADD [Status] int NOT NULL DEFAULT 1;
-- UPDATE [Surveys] SET [Status] = CASE WHEN [IsClosed] = 1 THEN -1 ELSE 1 END;
-- ALTER TABLE [Surveys] DROP COLUMN [IsClosed];
-- INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260316120000_ReplaceIsClosedWithStatus', N'8.0.11');
