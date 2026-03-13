# Advanced features – design and implementation guide

This document outlines how to add the following features to the Survey API. Each section covers data model changes, API behavior, and implementation notes.

---

## 1. Draft autosave

**Goal:** Save survey and question edits as drafts; support auto-save so work is not lost.

### Data model
- **Survey:** Add `Status` enum: `Draft`, `Published` (and optionally `Archived`). Add `LastSavedAt` (DateTime, nullable) for client-side sync.
- **SurveyPage:** Optionally `LastSavedAt`.
- **Question:** No change required; they are part of the survey draft.

### API
- **PATCH /api/surveys/{id}/draft** – Save survey metadata (title, description) as draft; set `Status = Draft`, `LastSavedAt = UtcNow`. Require Researcher/Admin.
- **PATCH /api/surveys/{id}/publish** – Set `Status = Published`; only published surveys accept responses. Require Researcher/Admin.
- **GET /api/surveys** – Optional query `?status=Draft|Published` to filter.
- **POST /api/responses** – Reject with 403 if `Survey.Status != Published`.
- **Question/Page create/update** – No change; they always persist. Client can call "save draft" (PATCH survey draft) after editing.

### Implementation notes
- Add `SurveyStatus` enum and `Status`, `LastSavedAt` to `Survey`; migration `AddSurveyDraftSupport`.
- In `ResponseService.SubmitAsync`, check `survey.Status == Published` before adding a response.
- Frontend: poll or debounce auto-save by calling PATCH draft and/or question/page endpoints.

---

## 2. Response limits

**Goal:** Cap the number of responses per survey (e.g. first 100 only).

### Data model
- **Survey:** Add `MaxResponses` (int, nullable). `null` = no limit.

### API
- **Create/Update survey:** Accept optional `MaxResponses` (null or positive int).
- **POST /api/responses** – Before creating a response, if `Survey.MaxResponses` is set, check `Survey.SurveyResponses.Count < MaxResponses`. Return 429 (Too Many Requests) or 400 with message when limit reached.

### Implementation notes
- Add `MaxResponses` to `Survey`, `CreateSurveyDto`, `UpdateSurveyDto`, `SurveyResponseDto`.
- In `ResponseService.SubmitAsync`: load survey, if `MaxResponses.HasValue` and `await _db.SurveyResponses.CountAsync(r => r.SurveyId == survey.Id) >= survey.MaxResponses` then return null or throw; caller returns 429.

---

## 3. Anonymous links

**Goal:** Share a survey via a link that does not require login; optionally track link (e.g. for campaigns).

### Current state
- **POST /api/responses** already allows unauthenticated submission (anonymous participation).
- **GET /api/surveys/{id}** and **GET /api/surveys/{id}/pages** can be called without auth to load the survey for participants.

### Enhancements
- **Survey:** Add `AllowAnonymous` (bool, default true) to explicitly allow/deny unauthenticated access to survey + pages + submit. If false, require JWT for GET survey/pages and for POST responses (and map response to user if needed).
- **Share link:** Base URL is client-defined (e.g. `https://yourapp.com/survey/42`). API only needs to serve GET survey/pages and POST responses; no token in link.
- **Optional – Trackable links:** Add table `SurveyLink` (Id, SurveyId, Slug or Token, Label, CreatedAt). Share URL: `.../s/{slug}`. Resolve slug to SurveyId; optional: store `SurveyLinkId` on `SurveyResponse` for analytics. API: GET /api/s/slug → redirect or return survey + page data; POST responses can accept optional `LinkId` or resolve from slug.

### API (minimal)
- Add `AllowAnonymous` to survey DTOs and model; GET survey/pages and POST responses check it when false (require auth).

### API (trackable links)
- **POST /api/surveys/{surveyId}/links** – Create link (SurveyId, Label, optional Slug). Return slug/token and full URL hint.
- **GET /api/s/{slugOrToken}** – Return survey info + pages (for display); 404 if not found or survey not published.
- **POST /api/responses** – Optional `LinkSlug` or `LinkId` to associate response with a link.

### Implementation notes
- Add `AllowAnonymous` to Survey; migration.
- Optional: add `SurveyLink` entity and `SurveyResponse.LinkId` (nullable FK); migration; endpoint to create link and resolve slug.

---

## 4. QR code sharing

**Goal:** Generate a QR code that encodes the survey URL for easy mobile sharing.

### Approach
- API does **not** store or serve images. API returns the **share URL** (and optionally a slug for short URLs); client or a separate service generates the QR image.
- **GET /api/surveys/{id}/share-url** – Returns `{ "url": "https://...", "slug": "abc12" }` (slug if you implement anonymous/trackable links). Frontend or a QR service (e.g. a public QR generator API) uses `url` to generate the QR image.

### Optional server-side QR
- Add package (e.g. `QRCoder` or `SkiaSharp` + QR lib). Endpoint **GET /api/surveys/{id}/qr** returns `image/png`. Cache or generate on demand from survey share URL.

### Implementation notes
- Implement share-url endpoint first; add QR generation only if you want it server-side.

---

## 5. Export

**Goal:** Export survey responses or summary (CSV, Excel, PDF).

### API
- **GET /api/surveys/{surveyId}/responses/export?format=csv** – Return CSV: one row per response, columns = SurveyId, ResponseId, SubmittedAt, ParticipantName, Q1_Text, Q2_Text, … (one column per question).
- **GET /api/surveys/{surveyId}/responses/export?format=json** – Return JSON array of responses with answers.
- **GET /api/surveys/{surveyId}/summary/export?format=csv** – Export summary (e.g. question-level aggregates) as CSV.
- **GET /api/surveys/{surveyId}/summary/export?format=pdf** – Optional: generate PDF (use a library such as QuestPDF or iTextSharp).

### Implementation notes
- Reuse existing `GetBySurveyIdAsync` and `GetSurveySummaryAsync`; serialize to CSV in a new `ExportService` or extension methods. Use `Content-Disposition: attachment; filename="survey-{id}-responses.csv"`.
- For PDF, add a package and a dedicated export method that builds the document from summary DTOs.

---

## 6. Branding

**Goal:** Custom logo, colors, or name per survey (or per researcher/tenant).

### Data model
- **Survey:** Add optional `BrandingJson` (string) or separate columns: `LogoUrl`, `PrimaryColor`, `SecondaryColor`, `OrganizationName`. Prefer nullable columns for simple query/filter.
- Alternatively **Researcher/User:** Add branding at user level so all their surveys share it (e.g. `User.BrandingJson` or LogoUrl, PrimaryColor, etc.).

### API
- **Survey:** Include branding fields in GET survey and in create/update DTOs. **GET /api/surveys/{id}** (and public survey view) returns branding for the client to apply.
- **PATCH /api/surveys/{id}/branding** – Optional dedicated endpoint for logo/colors/name only.

### Implementation notes
- Add columns to Survey (or User); migration. Return in survey DTOs. Frontend applies logo and colors to the survey UI.

---

## 7. Duplicate survey

**Goal:** Copy an existing survey (structure only, or structure + responses).

### API
- **POST /api/surveys/{id}/duplicate** – Body optional: `{ "includeResponses": false }`. Creates a new survey (same researcher): copy Title + " (Copy)", Description, Status (e.g. Draft), pages, questions (same Order, Type, Text, IsRequired, OptionsJson). If `includeResponses == true`, also copy SurveyResponses and Answers. Return new survey DTO (201).

### Implementation notes
- New method `SurveyService.DuplicateAsync(surveyId, researcherId, includeResponses)`. Load survey with Pages and Questions (and optionally SurveyResponses + Answers). New survey: new Id, CreatedAt = now, ResearcherId = current user; copy all other fields. Insert new Survey, then new SurveyPages (map old page Id → new page Id), then new Questions (SurveyId, PageId from map), then optionally new SurveyResponses and Answers (QuestionId from map). Single transaction. Ensure researcher owns the source survey.

---

## Summary table

| Feature           | Model changes                          | Key API(s)                                                    |
|------------------|----------------------------------------|---------------------------------------------------------------|
| Draft autosave   | Survey.Status, LastSavedAt             | PATCH survey draft/publish, GET ?status=, submit checks       |
| Response limits  | Survey.MaxResponses                    | Create/update survey, POST responses returns 429 when capped  |
| Anonymous links  | Survey.AllowAnonymous; optional SurveyLink | GET /api/s/{slug}, POST /surveys/{id}/links, submit optional LinkId |
| QR code          | None                                   | GET share-url; optional GET survey qr image                   |
| Export           | None                                   | GET .../responses/export?format=csv|json, .../summary/export     |
| Branding         | Survey (or User) logo/colors/name       | GET survey includes branding; PATCH survey or branding        |
| Duplicate survey | None                                   | POST /api/surveys/{id}/duplicate                              |

Implement in any order; draft + response limits + duplicate are the smallest and most self-contained.
