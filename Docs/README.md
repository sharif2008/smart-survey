# Survey API – Setup & Usage

## Migrations (Entity Framework Core)

Tables are defined by your entities and `AppDbContext` (`Data/AppDbContext.cs`). You create and apply migrations from the project folder.

### Install EF Core tools (once)

```bash
dotnet tool install --global dotnet-ef
```

### Create a migration (after model/DbContext changes)

From the project root (where the `.csproj` is):

```bash
dotnet ef migrations add YourMigrationName
```

Example: `dotnet ef migrations add AddSurveyQuestionsResponses`.

If the project has multiple DbContext types, specify the context:

```bash
dotnet ef migrations add YourMigrationName --context AppDbContext
```

### Apply all migrations (create/update database tables)

To apply **all** pending migrations to the database:

```bash
dotnet ef database update
```

With a specific context:

```bash
dotnet ef database update --context AppDbContext
```

Migrations are applied manually. Run `dotnet ef database update --context AppDbContext` before starting the app (or as part of deployment).

---

## Swagger (OpenAPI)

### When it’s available

- Swagger is enabled **only in Development** (`if (app.Environment.IsDevelopment())` in `Program.cs`).
- In Production, Swagger endpoints are not registered.

### URLs (when running locally)

- **Swagger UI:** `https://localhost:<port>/swagger` (or `http://localhost:<port>/swagger`).
- **OpenAPI JSON:** `https://localhost:<port>/swagger/v1/swagger.json`.

The exact port is shown in the console when you run the app (e.g. `https://localhost:5001`).

### Authentication in Swagger

- The API uses **JWT Bearer** authentication.
- In Swagger UI there is an **Authorize** (lock) button.
- **Security scheme:** `Bearer` (API Key in `Authorization` header).
- **How to use:** Click **Authorize**, enter your token in one of these forms:
  - `Bearer <your-jwt-token>`, or
  - Just `<your-jwt-token>` (some setups add "Bearer " automatically).
- After authorizing, all requests from the UI will include the `Authorization` header.

### Swagger configuration (Program.cs)

- **Document:** `v1`, title **Survey API**, version **v1**.
- **Security definition:** Bearer scheme, `Authorization` header, `ApiKey` type.
- **Security requirement:** Applied globally so protected endpoints show the lock icon and accept the token you enter in Authorize.

---

## Logging (file)

- **API requests** and **errors** are written to files under the `Logs/` folder (created next to the running app).
- **Paths** (configurable in `appsettings.json` → `Serilog`):
  - `Logs/api-.log` – all requests and info (rolling daily; date in filename).
  - `Logs/errors-.log` – errors only (rolling daily).
- Each request is logged as: `HTTP {Method} {Path} responded {StatusCode} in {Elapsed} ms`.
- Unhandled exceptions are logged by `ExceptionMiddleware` and appear in both logs and in `errors-.log`.

---

## Database

- **Provider:** Configurable via `DatabaseProvider` in `appsettings.json`: `"MySql"` or `"SqlServer"`. Connection string from `DefaultConnection`.
- **MySQL:** Set `DatabaseProvider` to `"MySql"` and `DefaultConnection` to your MySQL connection (e.g. `Server=localhost;Port=3306;Database=SurveyDb;User=root;Password=...;`). Ensure MySQL is running, then run `dotnet ef database update --context AppDbContext` to create tables.
- **SQL Server:** Set `DatabaseProvider` to `"SqlServer"` (or omit it) and `DefaultConnection` to your SQL Server connection.
- Answer text is stored in `Answers.ResponseText`; rating/number/date values are stored as strings and parsed when building summaries.
