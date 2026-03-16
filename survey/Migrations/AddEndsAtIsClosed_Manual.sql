-- Run this on your MySQL database if the migration cannot be applied (e.g. app is running).
-- Adds EndsAt and IsClosed to Surveys table.

ALTER TABLE `Surveys`
  ADD COLUMN `EndsAt` datetime(6) NULL,
  ADD COLUMN `IsClosed` tinyint(1) NOT NULL DEFAULT 0;

-- After the ALTER, run this so EF Core knows the migration was applied:
-- INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260316075021_AddSurveyEndsAtAndIsClosed', '8.0.11');
