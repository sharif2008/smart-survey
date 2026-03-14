# Survey REST API – Endpoints and Usage

## Backend API list (all implemented)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| **Auth** | | | |
| POST | `/api/auth/register` | No | Register (Admin or Researcher) |
| POST | `/api/auth/login` | No | Login, returns JWT |
| **Users** (Admin only) | | | |
| GET | `/api/users` | Admin | List all users |
| GET | `/api/users/{id}` | Admin | Get user by id |
| POST | `/api/users` | Admin | Create user |
| PUT | `/api/users/{id}` | Admin | Update user |
| DELETE | `/api/users/{id}` | Admin | Delete user |
| **Surveys** | | | |
| GET | `/api/surveys` | Admin, Researcher | List surveys (own or all) |
| GET | `/api/surveys/{id}` | No (public) | Get survey by id |
| POST | `/api/surveys` | Admin, Researcher | Create survey |
| PUT | `/api/surveys/{id}` | Admin, Researcher | Update survey |
| DELETE | `/api/surveys/{id}` | Admin, Researcher | Delete survey |
| **Survey pages** | | | |
| GET | `/api/surveys/{surveyId}/pages` | No | Get pages with nested questions |
| GET | `/api/surveys/{surveyId}/pages/{pageId}` | No | Get single page by id |
| POST | `/api/surveys/{surveyId}/pages` | Admin, Researcher | Create survey page |
| PUT | `/api/surveys/{surveyId}/pages/{pageId}` | Admin, Researcher | Update page |
| DELETE | `/api/surveys/{surveyId}/pages/{pageId}` | Admin, Researcher | Delete page |
| **Questions** | | | |
| GET | `/api/surveys/{surveyId}/questions` | No (public) | Get questions for a survey |
| POST | `/api/questions` | Admin, Researcher | Create question |
| PUT | `/api/questions/{id}` | Admin, Researcher | Update question |
| DELETE | `/api/questions/{id}` | Admin, Researcher | Delete question |
| **Responses** | | | |
| POST | `/api/responses` | No (participants) | Submit survey response |
| GET | `/api/surveys/{surveyId}/responses` | Admin, Researcher | List responses for a survey |
| **Analytics** | | | |
| GET | `/api/surveys/{surveyId}/summary` | Admin, Researcher | Survey summary/analytics |

## Database and migrations

- **Context:** `AppDbContext` (SQL Server).
- **Connection string** in `appsettings.json`:
  - Example: `Server=(localdb)\\mssqllocaldb;Database=SurveyDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`
- On first run, the app calls `EnsureCreatedAsync()` so the database and tables are created.
- For **migrations** (e.g. production):
  ```bash
  dotnet ef migrations add InitialCreate --context AppDbContext
  dotnet ef database update --context AppDbContext
  ```

## Seed data

- If no user with role `Admin` exists, the app seeds one:
  - **Email:** `admin@survey.local`
  - **Password:** `Admin@123`
  - **Role:** Admin

## Sample requests and responses

### 1. Register (Admin or Researcher)

```http
POST /api/auth/register
Content-Type: application/json

{
  "fullName": "Jane Researcher",
  "email": "jane@example.com",
  "password": "SecurePass123",
  "role": "Researcher"
}
```

`role` must be `"Admin"` or `"Researcher"`. **Response (200):** same shape as login (token + user). **Response (409):** email already exists.

### 2. Login (get JWT)

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "admin@survey.local", "password": "Admin@123" }
```

**Response (200):**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 1,
    "fullName": "Admin User",
    "email": "admin@survey.local",
    "role": "Admin",
    "createdAt": "2026-03-12T20:00:00Z"
  }
}
```

### 3. Create survey (Researcher/Admin, use token in header)

```http
POST /api/surveys
Authorization: Bearer <token>
Content-Type: application/json

{ "title": "Customer Feedback", "description": "Optional description" }
```

**Response (201):** `{ "id": 1, "title": "Customer Feedback", "description": "Optional description", "researcherId": 1, "createdAt": "..." }`

### 4. Add question

```http
POST /api/questions
Authorization: Bearer <token>
Content-Type: application/json

{
  "surveyId": 1,
  "text": "How would you rate the service?",
  "type": 6,
  "isRequired": true,
  "order": 1
}
```

`type`: 0=Text, 1=TextArea, 2=Number, 3=YesNo, 4=SingleChoice, 5=MultipleChoice, 6=Rating, 7=Date

### 5. Submit response (no auth – participant)

```http
POST /api/responses
Content-Type: application/json

{
  "surveyId": 1,
  "participantName": "John Doe",
  "answers": [
    { "questionId": 1, "responseText": "5" }
  ]
}
```

**Response (201):** `{ "surveyResponseId": 1, "surveyId": 1, "submittedAt": "..." }`

### 6. Get survey summary (Admin/Researcher)

```http
GET /api/surveys/1/summary
Authorization: Bearer <token>
```

**Response (200):** JSON with `surveyId`, `surveyTitle`, `totalResponses`, `generatedAt`, `questions` (each with `questionId`, `questionText`, `questionType`, `totalAnswers`, `summary`).

## Swagger

- **URL:** `/swagger` or `/swagger/index.html` (e.g. `http://localhost:5008/swagger` in Development).
- Use **Authorize** and enter `Bearer <your_token>` to call protected endpoints.
