# Survey API – Setup & Usage

## Branching / conditional logic (Show If)

Questions can be shown only when a previous answer matches a condition (SurveyMonkey-style).

- **Storage:** Each question has an optional `showIf` object stored as JSON in the database.
- **API:** When you GET questions (e.g. `GET /api/surveys/{id}/pages` or `GET /api/surveys/{id}/questions`), each question includes a **showIf** object when set:
  ```json
  {
    "questionId": 1,
    "operator": "equals",
    "value": "Yes"
  }
  ```
- **Client responsibility:** The client evaluates conditions as the user answers. When rendering the next question, check the answer to `showIf.questionId` against `showIf.value` using `showIf.operator` (e.g. `equals`, `notEquals`, `contains`, `lessThan`, `greaterThan`). If the condition is true, show the question; otherwise hide or skip it.
- **Create/update:** When creating or updating a question (POST/PUT), send **showIf** in the body to set or clear the condition. Omit or `null` for no condition (question always shown).

Supported operators can be extended later (e.g. in the client or via a shared constants list). The API stores and returns the structure; evaluation is client-side.

---

## Question types and validation

### Question types (enum)
- **Text** (0) – single-line text
- **TextArea** (1) – multi-line text
- **Number** (2) – numeric value
- **YesNo** (3) – yes/no
- **SingleChoice** (4) – one option from a list (options in `optionsJson`)
- **MultipleChoice** (5) – multiple options (options in `optionsJson`; answer can be comma-separated)
- **Rating** (6) – numeric rating
- **Date** (7) – date value

### Validation (per question)
Each question can have a **validation** object (stored as JSON). Only set properties apply. The API validates on **POST /api/responses** and returns 400 with an `errors` array when validation fails.

| Rule | Meaning | Applies to |
|------|--------|------------|
| **required** | Must not be empty | All |
| **minLength** / **maxLength** | Character length | Text, TextArea |
| **regex** | Pattern match | Text, TextArea |
| **minNumber** / **maxNumber** | Numeric range | Number, Rating |
| **dateMin** / **dateMax** | Date range (ISO date string) | Date |
| **optionMustExist** | Answer must be one of the question options | SingleChoice, MultipleChoice |
| **maxSelectionCount** | Max number of options selected | MultipleChoice |

Example (create/update question):
```json
{
  "surveyId": 1,
  "text": "Rate 1-5",
  "type": 6,
  "isRequired": true,
  "order": 1,
  "validation": {
    "required": true,
    "minNumber": 1,
    "maxNumber": 5
  }
}
```

Responses that fail validation return **400 Bad Request** with body: `{ "message": "Validation failed.", "errors": ["Question 2 is required.", "Question 3: maximum length is 500."] }`.

---

## Analytics and dynamic summary

**GET /api/surveys/{surveyId}/summary** (Admin/Researcher) returns analytics with one entry per question. The summary is **generated automatically and dynamically by inspecting the question type**—no separate configuration is needed. Each question’s `summary` object has a shape that matches its type:

| Type | Analytics (automatic per type) |
|------|-------------------------------|
| **Text / TextArea** | Response count, sample answers, frequent words (word + count; top 20). Also `topRepeated` (exact repeated responses with count). |
| **Yes/No** | Yes count, no count, percentages. |
| **Single choice** | Count per option, percentages, bar chart data (label, count). |
| **Multiple choice** | Count per option, percentages (% of respondents who selected each option). |
| **Rating** | Average, min, max, distribution (value → count). |
| **Number** | Min, max, average. |
| **Date** | Grouped by day, by week, by month (each: date/period string, count). Also earliest and latest. |

Rating and number support decimal values. Date grouping: by day (yyyy-MM-dd), by week (Monday-based week start, yyyy-MM-dd), by month (yyyy-MM).

---

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
