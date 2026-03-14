# Database migration scripts

## Contents

- **InitialCreate.MySql.sql** – MySQL 8/9: creates the full schema (Roles, Users, Surveys, Questions, SurveyResponses, Answers) and `__EFMigrationsHistory` using MySQL types (`longtext`, `datetime(6)`, `AUTO_INCREMENT`, etc.). Use when not applying migrations via EF.

## How to use

### Option 1: Apply via EF (recommended)

With `DatabaseProvider` set to `MySql` and your MySQL connection in config:

```bash
dotnet ef database update --context AppDbContext
```

### Option 2: Run the script manually

Run against your MySQL database (e.g. SurveyDb):

```bash
mysql -u root -p SurveyDb < Scripts/InitialCreate.MySql.sql
```

Or open `InitialCreate.MySql.sql` in MySQL Workbench or another client and execute it.

The script is idempotent: it uses `CREATE TABLE IF NOT EXISTS` and `INSERT IGNORE` for the migration history row.
