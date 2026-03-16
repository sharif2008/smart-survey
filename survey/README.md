## Survey API

Backend REST API for building, running, and analyzing online surveys. It supports authenticated survey authors (Admins / Researchers) and anonymous or authenticated participants.

- **Tech stack**: ASP.NET Core, Entity Framework Core, SQL Server / MySQL, JWT auth, Serilog logging.
- **Main capabilities**: user management, survey + page + question management, dynamic validation, conditional logic (“show if”), analytics, export, and response rate limiting.

---

## High‑level architecture

- **API project**: `SurveyApi` (this folder).
- **Data layer**: `AppDbContext` with entities for `User`, `Role`, `Survey`, `SurveyPage`, `Question`, `SurveyResponse`, `Answer`.
- **Services**:
  - `AuthService`, `TokenService`, `UserService`
  - `SurveyService`, `SurveyPageService`, `QuestionService`, `ResponseService`
  - `DashboardService`, `ExportService`
- **Controllers**:
  - `AuthController`, `UsersController`, `ProfileController`
  - `SurveysController`, `SurveyPagesController`, `QuestionsController`, `ResponsesController`
  - `DashboardController`, `AnalyticsController`
- **Cross‑cutting**:
  - `ExceptionMiddleware` – consistent error responses + logging
  - `SubmissionRateLimitMiddleware` – throttles incoming responses
  - `AnswerValidator`, `ValidationHelper`, `ShowIfHelper` – validation and conditional logic helpers

Swagger is enabled in **Development** for discovery and manual testing.

---

## ERD – data model

**Main entities and relationships:**

- **Role**
  - `Role (1) ────< (many) User`
- **User**
  - One `User` has one `Role` (`RoleId` FK).
  - One Researcher `User (1) ────< (many) Survey` (via `ResearcherId`).
- **Survey**
  - `Survey (1) ────< (many) SurveyPage`
  - `Survey (1) ────< (many) Question`
  - `Survey (1) ────< (many) SurveyResponse`
- **SurveyPage**
  - Belongs to one `Survey` (`SurveyId`).
  - `SurveyPage (1) ────< (many) Question` (via `PageId`).
- **Question**
  - Belongs to one `Survey` and one `SurveyPage`.
  - `Question (1) ────< (many) Answer`.
- **SurveyResponse**
  - Belongs to one `Survey`.
  - `SurveyResponse (1) ────< (many) Answer`.
- **Answer**
  - Belongs to one `SurveyResponse` and one `Question`.

Text ERD (simplified):

```text
Role (1) ────< (many) User

User (1) ────< (many) Survey

Survey (1) ────< (many) SurveyPage
Survey (1) ────< (many) Question
Survey (1) ────< (many) SurveyResponse

SurveyPage (1) ────< (many) Question

SurveyResponse (1) ────< (many) Answer
Question (1) ────< (many) Answer
```

**Table summary**

- **Role**
  - `Id` (PK), `Name`
- **User**
  - `Id` (PK), `FullName`, `Email`, `PasswordHash`, `RoleId` (FK → `Role.Id`), `CreatedAt`
- **Survey**
  - `Id` (PK), `Title`, `Description`, `ResearcherId` (FK → `User.Id`), `CreatedAt`, `EndsAt`, `IsClosed`
- **SurveyPage**
  - `Id` (PK), `SurveyId` (FK → `Survey.Id`), `Title`, `Description`, `Order`
- **Question**
  - `Id` (PK), `SurveyId` (FK → `Survey.Id`), `PageId` (FK → `SurveyPage.Id`), `Text`, `Type`, `IsRequired`, `Order`, `OptionsJson`, `ShowIfJson`, `ValidationJson`
- **SurveyResponse**
  - `Id` (PK), `SurveyId` (FK → `Survey.Id`), `ParticipantName`, `SubmittedAt`
- **Answer**
  - `Id` (PK), `SurveyResponseId` (FK → `SurveyResponse.Id`), `QuestionId` (FK → `Question.Id`), `ResponseText`

---

## Features overview

- **Authentication & authorization**
  - **What**: JWT‑based auth with roles (`Admin`, `Researcher`, `Participant`).
  - **How**: `POST /api/auth/register` and `POST /api/auth/login` issue a JWT; controllers use `[Authorize]` and role policies so only the correct users can call admin/researcher endpoints.

- **User management (Admin)**
  - **What**: Full CRUD for users plus role assignment.
  - **How**: Admins call `/api/users` to list, create, update, and delete users. A default Admin (`admin@survey.local` / `Admin@123`) is seeded on first run so you can log in and bootstrap the system.

- **Survey lifecycle**
  - **What**: Creation and management of multi‑page surveys that can be drafted, published, closed by date, or closed manually.
  - **How**:
    - Create/update/delete via `/api/surveys`. Delete is **soft** (sets `DeletedAt`); deleted surveys are excluded from all queries and from list/get-by-id.
    - Structure via `/api/surveys/{surveyId}/pages` and `/api/questions`.
    - Use `EndsAt` and `IsClosed` to stop accepting responses; advanced design also supports explicit `Status` + `MaxResponses` (see `Docs/ADVANCED-FEATURES.md`).

- **Question types (what & how they behave)**
  - **Text (0)** – Single‑line free‑text input.
    - Use for short answers like names or titles; validated by `required`, `minLength`, `maxLength`, `regex`.
  - **TextArea (1)** – Multi‑line free‑text input.
    - Use for comments/feedback; same text validation rules as `Text`, good for qualitative answers.
  - **Number (2)** – Numeric value.
    - Use for quantities (age, count, amount); validated via `minNumber`/`maxNumber`. Stored as string in `ResponseText` and parsed for analytics.
  - **YesNo (3)** – Boolean yes/no.
    - Use for toggles or binary decisions; analytics calculate counts and percentages of yes vs no.
  - **SingleChoice (4)** – One option from a list.
    - Options are stored in `OptionsJson`; participant picks exactly one, and analytics compute counts and percentages per option.
  - **MultipleChoice (5)** – Multiple options from a list.
    - Options in `OptionsJson`; multiple selections are stored as comma‑separated values in `ResponseText`. Validation can enforce `optionMustExist` and `maxSelectionCount`. Analytics show how many respondents picked each option.
  - **Rating (6)** – Numeric rating (e.g. 1–5, 1–10).
    - Use for Likert‑style questions; validated with `minNumber`/`maxNumber`. Analytics compute average, min, max, and distribution.
  - **Date (7)** – Date value.
    - Use for dates (visit date, event date); validated with `dateMin`/`dateMax`. Analytics group by day, week, and month and expose earliest/latest.

- **Validation (per question)**
  - **What**: Configurable rules per question to keep data clean.
  - **How**: Each question has `ValidationJson` stored as JSON with fields:
    - `required`, `minLength`, `maxLength`, `regex`
    - `minNumber`, `maxNumber`
    - `dateMin`, `dateMax`
    - `optionMustExist`, `maxSelectionCount`
  - When a participant submits via `POST /api/responses`, the server applies these rules (using `AnswerValidator` / `ValidationHelper`) and returns `400` with an `errors` array if any rule fails.

- **Conditional logic (“Show If”)**
  - **What**: Show or hide a question based on previous answers (branching logic).
  - **How**:
    - Each question can include `ShowIfJson` (e.g. `{ "questionId": 1, "operator": "equals", "value": "Yes" }`).
    - API returns this structure when you fetch questions/pages; the **client** evaluates the condition as the user answers and decides whether to render the dependent question.

- **Responses & submissions**
  - **What**: Store participants’ answers as `SurveyResponse` + `Answer` records.
  - **How**:
    - Frontend loads survey and structure via public GET endpoints.
    - On submit, frontend posts to `POST /api/responses` with `surveyId` and `answers[]`.
    - Middleware and services can enforce caps (e.g. max responses), block late responses (`EndsAt` / `IsClosed`), and apply rate limiting.

- **Analytics & dashboard**
  - **What**: Automatic summaries per question and high‑level dashboards.
  - **How**:
    - `GET /api/surveys/{surveyId}/summary` inspects each question’s `Type` and builds a typed `summary` object automatically.
    - Text/TextArea → counts, samples, frequent words, repeated responses.
    - Yes/No → yes/no counts and percentages.
    - Choice/MultipleChoice → counts and percentages per option.
    - Rating/Number → min, max, average, distribution.
    - Date → grouped counts by day/week/month plus earliest/latest.
    - Dashboard endpoints aggregate across surveys for quick KPI views.

- **Export**
  - **What**: Download raw responses or computed summaries as files.
  - **How**:
    - `ExportService` converts responses or summaries to CSV/JSON.
    - Endpoints like `GET /api/surveys/{surveyId}/responses/export?format=csv|json` and `GET /api/surveys/{surveyId}/summary/export?format=csv|pdf` (if PDF is enabled) return downloadable files for offline analysis.

- **Duplicate submissions, partial save & resume (design)**
  - **Avoiding duplicate submissions**
    - **What**: Prevent the same participant from submitting the same survey multiple times (e.g. double clicks, refresh spam).
    - **How**:
      - Frontend generates a **client submission id** (e.g. UUID) and sends it on `POST /api/responses`.
      - Backend stores this id on `SurveyResponse` (e.g. `ClientToken`) with a **unique index** per survey; if the same token is seen again, return the existing response instead of creating a new one.
      - Optionally also limit by participant identifier (email or user id) + survey to enforce one response per user.
  - **Partial submit & resume**
    - **What**: Let participants save progress and continue later.
    - **How**:
      - Introduce a `status` field on `SurveyResponse` (e.g. `Draft`, `Completed`) and allow **partial responses** to be saved via a dedicated endpoint like `POST /api/responses/draft` or by allowing `POST /api/responses` with `status=Draft`.
      - The client stores a **resume token** (response id or opaque GUID) in local storage or query string.
      - On resume, the client calls `GET /api/responses/{id}` (or `GET /api/surveys/{surveyId}/responses/me`) to load saved answers and prefill the form; submitting again with `status=Completed` finalizes the response.
      - Server can decide whether drafts count toward rate limits / max responses; typically they do **not** until marked `Completed`.

- **Branding & sharing (design)**
  - **What**: Custom look‑and‑feel per survey and sharable links/QR codes.
  - **How** (design in docs):
    - Add branding fields on `Survey` or `User` (logo URL, colors, organization name).
    - Create anonymous, trackable links and QR codes from survey share URLs.
    - Details and suggested endpoints are documented in `Docs/ADVANCED-FEATURES.md`.

- **Logging & diagnostics**
  - **What**: Operational insight into requests and errors.
  - **How**:
    - Serilog writes request logs and errors to `Logs/api-*.log` and `Logs/errors-*.log`.
    - `ExceptionMiddleware` turns unexpected exceptions into consistent error responses and logs full details for troubleshooting.

---

## Environment & configuration

Key configuration lives in `appsettings.json` / `appsettings.Development.json` and environment variables:

- **Database**
  - `DatabaseProvider`: `"SqlServer"` (default) or `"MySql"`.
  - Connection string under `ConnectionStrings:DefaultConnection`.
  - MySQL support uses Pomelo provider; on startup, additional `ALTER TABLE` commands ensure `AUTO_INCREMENT` on key columns.

- **JWT**
  - `Jwt:Key` – symmetric signing key (at least 32 chars).
  - `Jwt:Issuer`, `Jwt:Audience` – issuer and audience used in token validation.
  - Defaults exist for local development but **must be overridden in production**.

- **Serilog**
  - `Serilog:LogPath` (e.g. `Logs/api-.log`).
  - `Serilog:ErrorLogPath` (e.g. `Logs/errors-.log`).

---

## Setup & running locally

1. **Restore and build**
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Configure database**
   - Choose provider and connection string in `appsettings.Development.json`:
     - SQL Server example:
       ```json
       "DatabaseProvider": "SqlServer",
       "ConnectionStrings": {
         "DefaultConnection": "Server=(localdb)\\\\mssqllocaldb;Database=SurveyDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
       }
       ```
     - MySQL example:
       ```json
       "DatabaseProvider": "MySql",
       "ConnectionStrings": {
         "DefaultConnection": "Server=localhost;Port=3306;Database=SurveyDb;User=root;Password=yourpassword;"
       }
       ```

3. **Apply migrations**
   ```bash
   dotnet tool install --global dotnet-ef   # once
   dotnet ef database update --context AppDbContext
   ```

4. **Run the API**
   ```bash
   dotnet run
   ```

5. **Swagger (development only)**
   - Browse to `http://localhost:<port>/swagger`.
   - Use the **Authorize** button with `Bearer <your-jwt-token>` for secured endpoints.

On first run, an **Admin** user is seeded:

- Email: `admin@survey.local`
- Password: `Admin@123`

---

## Core API surface & roles

A complete endpoint table with examples is in `README_API.md`. High‑level groups:

- **Auth**
  - `POST /api/auth/register` – create Admin/Researcher.
  - `POST /api/auth/login` – get JWT.

- **Users (Admin only)**
  - `GET /api/users`, `GET /api/users/{id}`, `POST /api/users`, `PUT /api/users/{id}`, `DELETE /api/users/{id}`.

- **Surveys**
  - `GET /api/surveys` – list surveys.
  - `GET /api/surveys/{id}` – public survey details.
  - `POST /api/surveys`, `PUT /api/surveys/{id}`, `DELETE /api/surveys/{id}`.

- **Survey pages**
  - `GET /api/surveys/{surveyId}/pages` – pages with nested questions (public, for participants).
  - `GET /api/surveys/{surveyId}/pages/{pageId}`.
  - `POST /api/surveys/{surveyId}/pages`, `PUT /api/surveys/{surveyId}/pages/{pageId}`, `DELETE /api/surveys/{surveyId}/pages/{pageId}`.

- **Questions**
  - `GET /api/surveys/{surveyId}/questions`.
  - `POST /api/questions`, `PUT /api/questions/{id}`, `DELETE /api/questions/{id}`.

- **Responses (participants)**
  - `POST /api/responses` – submit a filled survey.
  - `GET /api/surveys/{surveyId}/responses` – list responses (Admin/Researcher).

- **Analytics & dashboard**
  - `GET /api/surveys/{surveyId}/summary` – per‑question analytics.
  - Additional dashboard endpoints under `/api/dashboard` and `/api/analytics`.

- **Export**
  - `GET /api/surveys/{surveyId}/responses/export?format=csv|json`.
  - `GET /api/surveys/{surveyId}/summary/export?format=csv|pdf` (if PDF is enabled).

See `README_API.md` and `Docs/README.md` for exact request/response shapes.

---

## Typical workflows

### 1. Admin – initial setup

1. Run the API; seeded Admin user is created.
2. Login via `POST /api/auth/login` using the seeded credentials.
3. Use the token to:
   - Create additional Admin / Researcher accounts via `/api/users` or `/api/auth/register`.
   - Configure system‑level settings (branding, limits, etc. as needed).

### 2. Researcher – create and publish a survey

1. **Login**
   - `POST /api/auth/login` with Researcher credentials; store JWT.

2. **Create survey**
   - `POST /api/surveys` with title, description, and (optionally) advanced settings such as `MaxResponses`.

3. **Define structure**
   - Create pages: `POST /api/surveys/{surveyId}/pages`.
   - Add questions:
     - `POST /api/questions` with `surveyId`, `pageId` (if used), `type`, `order`, `validation`, `showIf`, and `optionsJson` (for choice questions).

4. **Preview in client**
   - Client calls public endpoints:
     - `GET /api/surveys/{id}`
     - `GET /api/surveys/{surveyId}/pages` or `GET /api/surveys/{surveyId}/questions`
   - Client renders pages and questions, evaluates `showIf`, and applies validation hints.

5. **Publish and share**
   - Mark survey as published (see advanced draft/publish design).
   - Share link to your frontend (e.g. `https://app.yourdomain.com/surveys/{id}`) which uses the public API to load content.
   - Optionally generate QR codes or short links (per `Docs/ADVANCED-FEATURES.md`).

### 3. Participant – fill out a survey

1. Open the survey link in the frontend.
2. Frontend calls:
   - `GET /api/surveys/{id}`
   - `GET /api/surveys/{surveyId}/pages` (or questions) to render form.
3. Participant answers questions; frontend:
   - Applies client‑side validation using question `validation` JSON.
   - Evaluates `showIf` for conditional questions.
4. On submit, frontend sends:
   - `POST /api/responses` with `surveyId`, optional participant metadata, and array of answers.
5. Backend validates, persists, and returns submission metadata.

### 4. Researcher – analyze results

1. Fetch raw responses:
   - `GET /api/surveys/{surveyId}/responses`.
2. Fetch summary analytics:
   - `GET /api/surveys/{surveyId}/summary` for automatic per‑question metrics:
     - Counts, averages, distributions, date groupings, word frequency, etc.
3. Export:
   - `GET /api/surveys/{surveyId}/responses/export?format=csv` for row‑level data.
   - `GET /api/surveys/{surveyId}/summary/export?format=csv` (and `pdf` if implemented).
4. Use dashboard endpoints for cross‑survey views and KPIs.

---

## Advanced & extension points

Implementation guidance for advanced features lives in:

- `Docs/README.md` – question types, validation, analytics, migrations, Swagger, logging, database configuration.
- `Docs/ADVANCED-FEATURES.md` – draft autosave, response limits, anonymous/trackable links, QR code sharing, export details, branding, duplicate survey.

These docs describe how to evolve the platform without breaking existing behaviors.

---

## Production notes

- **Security**
  - Always override `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` in production.
  - Use HTTPS termination (reverse proxy or Kestrel configuration).
  - Restrict CORS origins to trusted frontends (the default dev config allows localhost).

- **Operations**
  - Ensure log rotation and retention policies for the `Logs` directory.
  - Run database migrations as part of your deployment pipeline.
  - Monitor errors via `errors-*.log` and any attached APM / monitoring stack.

This README, together with `README_API.md` and the `Docs` folder, should give a complete view of the system’s features and end‑to‑end workflows.

