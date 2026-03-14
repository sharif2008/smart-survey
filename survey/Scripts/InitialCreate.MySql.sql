-- MySQL 8/9 compatible schema for Survey API (run on empty database).
-- Use this script only when running SQL manually. Prefer: dotnet ef database update (with DatabaseProvider=MySql).

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `Roles` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(50) NOT NULL,
    PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `Users` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FullName` varchar(200) NOT NULL,
    `Email` varchar(256) NOT NULL,
    `PasswordHash` longtext NOT NULL,
    `RoleId` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Users_RoleId` (`RoleId`),
    CONSTRAINT `FK_Users_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `Surveys` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` varchar(500) NOT NULL,
    `Description` longtext NULL,
    `ResearcherId` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Surveys_ResearcherId` (`ResearcherId`),
    CONSTRAINT `FK_Surveys_Users_ResearcherId` FOREIGN KEY (`ResearcherId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `Questions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SurveyId` int NOT NULL,
    `Text` varchar(2000) NOT NULL,
    `Type` int NOT NULL,
    `IsRequired` tinyint(1) NOT NULL,
    `Order` int NOT NULL,
    `OptionsJson` longtext NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Questions_SurveyId` (`SurveyId`),
    CONSTRAINT `FK_Questions_Surveys_SurveyId` FOREIGN KEY (`SurveyId`) REFERENCES `Surveys` (`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `SurveyResponses` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SurveyId` int NOT NULL,
    `ParticipantName` varchar(200) NULL,
    `SubmittedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_SurveyResponses_SurveyId` (`SurveyId`),
    CONSTRAINT `FK_SurveyResponses_Surveys_SurveyId` FOREIGN KEY (`SurveyId`) REFERENCES `Surveys` (`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `Answers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SurveyResponseId` int NOT NULL,
    `QuestionId` int NOT NULL,
    `ResponseText` varchar(4000) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Answers_QuestionId` (`QuestionId`),
    KEY `IX_Answers_SurveyResponseId` (`SurveyResponseId`),
    CONSTRAINT `FK_Answers_Questions_QuestionId` FOREIGN KEY (`QuestionId`) REFERENCES `Questions` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Answers_SurveyResponses_SurveyResponseId` FOREIGN KEY (`SurveyResponseId`) REFERENCES `SurveyResponses` (`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260313071607_InitialCreate', '8.0.11');
