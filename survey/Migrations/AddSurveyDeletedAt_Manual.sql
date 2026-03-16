-- Run this on your database if the migration cannot be applied (e.g. app is running).
-- Adds soft-delete column DeletedAt to Surveys table.

-- MySQL:
ALTER TABLE `Surveys` ADD COLUMN `DeletedAt` datetime(6) NULL;
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260316102116_AddSurveyDeletedAt', '8.0.11');

-- SQL Server (run only one of the two blocks):
-- ALTER TABLE [Surveys] ADD [DeletedAt] datetime2 NULL;
-- INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260316102116_AddSurveyDeletedAt', N'8.0.11');
