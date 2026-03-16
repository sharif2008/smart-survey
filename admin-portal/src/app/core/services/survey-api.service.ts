import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, map, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  SurveyDto,
  CreateSurveyDto,
  UpdateSurveyDto,
  QuestionDto,
  SurveyResponseListItemDto,
  SurveySummaryDto,
  SurveyPageDto,
  CreateSurveyPageDto,
  UpdateSurveyPageDto,
  CreateQuestionDto,
  UpdateQuestionDto,
  SubmitSurveyResponseDto,
  SurveySubmissionResultDto,
  SurveyResponseDetailDto
} from '../api/survey-api.model';

/** Normalize survey from API (handles PascalCase or camelCase). */
function toSurveyDto(raw: Record<string, unknown>): SurveyDto {
  const desc = raw['description'] ?? raw['Description'];
  return {
    id: Number((raw['id'] ?? raw['Id']) ?? 0),
    title: String(raw['title'] ?? raw['Title'] ?? ''),
    description: desc != null && typeof desc === 'string' ? desc : undefined,
    researcherId: Number((raw['researcherId'] ?? raw['ResearcherId']) ?? 0),
    createdAt: String(raw['createdAt'] ?? raw['CreatedAt'] ?? '')
  };
}

/** Normalize question from API (handles PascalCase or camelCase) so pageId/order are always set. */
function toQuestionDto(raw: Record<string, unknown>): QuestionDto {
  const showIfRaw = raw['showIf'] ?? raw['ShowIf'];
  const valRaw = raw['validation'] ?? raw['Validation'];
  let showIf: QuestionDto['showIf'];
  if (showIfRaw != null && typeof showIfRaw === 'object' && !Array.isArray(showIfRaw)) {
    const s = showIfRaw as Record<string, unknown>;
    showIf = {
      questionId: Number((s['questionId'] ?? s['QuestionId']) ?? 0),
      operator: String(s['operator'] ?? s['Operator'] ?? 'equals'),
      value: s['value'] ?? s['Value'] != null ? String(s['value'] ?? s['Value']) : undefined
    };
  }
  let validation: QuestionDto['validation'] | undefined;
  if (valRaw != null && typeof valRaw === 'object' && !Array.isArray(valRaw)) {
    const v = valRaw as Record<string, unknown>;
    const val: NonNullable<QuestionDto['validation']> = {};
    if (v['minLength'] ?? v['MinLength'] != null) val.minLength = Number(v['minLength'] ?? v['MinLength']);
    if (v['maxLength'] ?? v['MaxLength'] != null) val.maxLength = Number(v['maxLength'] ?? v['MaxLength']);
    if (v['regex'] ?? v['Regex'] != null) val.regex = String(v['regex'] ?? v['Regex'] ?? '');
    if (v['minNumber'] ?? v['MinNumber'] != null) val.minNumber = Number(v['minNumber'] ?? v['MinNumber']);
    if (v['maxNumber'] ?? v['MaxNumber'] != null) val.maxNumber = Number(v['maxNumber'] ?? v['MaxNumber']);
    validation = Object.keys(val).length > 0 ? val : undefined;
  }
  return {
    id: Number((raw['id'] ?? raw['Id']) ?? 0),
    surveyId: Number((raw['surveyId'] ?? raw['SurveyId']) ?? 0),
    pageId: Number((raw['pageId'] ?? raw['PageId']) ?? 0),
    text: String(raw['text'] ?? raw['Text'] ?? ''),
    type: Number((raw['type'] ?? raw['Type']) ?? 0),
    isRequired: Boolean(raw['isRequired'] ?? raw['IsRequired']),
    order: Number((raw['order'] ?? raw['Order']) ?? 0),
    optionsJson: raw['optionsJson'] ?? raw['OptionsJson'] != null ? String(raw['optionsJson'] ?? raw['OptionsJson']) : undefined,
    showIf,
    validation
  };
}

@Injectable({ providedIn: 'root' })
export class SurveyApiService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getSurveys(): Observable<SurveyDto[]> {
    return this.http.get<unknown[]>(`${this.apiUrl}/api/surveys`).pipe(
      map((list) => (Array.isArray(list) ? list.map((s) => toSurveyDto(s as Record<string, unknown>)) : [])),
      catchError((err) => throwError(() => err))
    );
  }

  getSurvey(id: number): Observable<SurveyDto | null> {
    return this.http.get<unknown>(`${this.apiUrl}/api/surveys/${id}`).pipe(
      map((body) => (body && typeof body === 'object' ? toSurveyDto(body as Record<string, unknown>) : null)),
      catchError(() => of(null))
    );
  }

  createSurvey(dto: CreateSurveyDto): Observable<SurveyDto | null> {
    return this.http.post<unknown>(`${this.apiUrl}/api/surveys`, dto).pipe(
      map((body) => (body && typeof body === 'object' ? toSurveyDto(body as Record<string, unknown>) : null)),
      catchError(() => of(null))
    );
  }

  updateSurvey(id: number, dto: UpdateSurveyDto): Observable<SurveyDto | null> {
    return this.http.put<unknown>(`${this.apiUrl}/api/surveys/${id}`, dto).pipe(
      map((body) => (body && typeof body === 'object' ? toSurveyDto(body as Record<string, unknown>) : null)),
      catchError(() => of(null))
    );
  }

  deleteSurvey(id: number): Observable<boolean> {
    return this.http
      .delete(`${this.apiUrl}/api/surveys/${id}`, { observe: 'response' })
      .pipe(
        map((r) => r.status === 204),
        catchError(() => of(false))
      );
  }

  getQuestions(surveyId: number): Observable<QuestionDto[]> {
    return this.http.get<unknown[]>(`${this.apiUrl}/api/surveys/${surveyId}/questions`).pipe(
      map((list) => (Array.isArray(list) ? list.map((q) => toQuestionDto(q as Record<string, unknown>)) : [])),
      catchError(() => of([]))
    );
  }

  getResponses(surveyId: number): Observable<SurveyResponseListItemDto[]> {
    return this.http
      .get<SurveyResponseListItemDto[]>(`${this.apiUrl}/api/surveys/${surveyId}/responses`)
      .pipe(catchError(() => of([])));
  }

  /** Download responses export (CSV or JSON) as a file. */
  exportResponses(surveyId: number, format: 'csv' | 'json'): Observable<Blob> {
    const fmt = format === 'json' ? 'json' : 'csv';
    return this.http.get(`${this.apiUrl}/api/surveys/${surveyId}/responses/export?format=${fmt}`, {
      responseType: 'blob'
    });
  }

  getResponseDetails(surveyId: number): Observable<SurveyResponseDetailDto[]> {
    return this.http
      .get<SurveyResponseDetailDto[]>(`${this.apiUrl}/api/surveys/${surveyId}/responses/details`)
      .pipe(catchError(() => of([])));
  }

  getSummary(surveyId: number): Observable<SurveySummaryDto | null> {
    return this.http
      .get<SurveySummaryDto>(`${this.apiUrl}/api/surveys/${surveyId}/summary`)
      .pipe(catchError(() => of(null)));
  }

  // Survey pages (README_API)
  getPages(surveyId: number): Observable<SurveyPageDto[]> {
    return this.http
      .get<SurveyPageDto[]>(`${this.apiUrl}/api/surveys/${surveyId}/pages`)
      .pipe(catchError(() => of([])));
  }

  getPage(surveyId: number, pageId: number): Observable<SurveyPageDto | null> {
    return this.http
      .get<SurveyPageDto>(`${this.apiUrl}/api/surveys/${surveyId}/pages/${pageId}`)
      .pipe(catchError(() => of(null)));
  }

  createPage(surveyId: number, dto: Omit<CreateSurveyPageDto, 'surveyId'>): Observable<SurveyPageDto | null> {
    return this.http
      .post<SurveyPageDto>(`${this.apiUrl}/api/surveys/${surveyId}/pages`, { ...dto, surveyId })
      .pipe(catchError(() => of(null)));
  }

  updatePage(surveyId: number, pageId: number, dto: UpdateSurveyPageDto): Observable<SurveyPageDto | null> {
    return this.http
      .put<SurveyPageDto>(`${this.apiUrl}/api/surveys/${surveyId}/pages/${pageId}`, dto)
      .pipe(catchError(() => of(null)));
  }

  deletePage(surveyId: number, pageId: number): Observable<boolean> {
    return this.http
      .delete(`${this.apiUrl}/api/surveys/${surveyId}/pages/${pageId}`, { observe: 'response' })
      .pipe(
        map((r) => r.status === 204),
        catchError(() => of(false))
      );
  }

  // Questions create/update/delete (README_API)
  createQuestion(dto: CreateQuestionDto): Observable<QuestionDto | null> {
    return this.http
      .post<QuestionDto>(`${this.apiUrl}/api/questions`, dto)
      .pipe(catchError(() => of(null)));
  }

  updateQuestion(id: number, dto: UpdateQuestionDto): Observable<QuestionDto | null> {
    return this.http
      .put<QuestionDto>(`${this.apiUrl}/api/questions/${id}`, dto)
      .pipe(catchError(() => of(null)));
  }

  deleteQuestion(id: number): Observable<boolean> {
    return this.http
      .delete(`${this.apiUrl}/api/questions/${id}`, { observe: 'response' })
      .pipe(
        map((r) => r.status === 204),
        catchError(() => of(false))
      );
  }

  /** Submit survey response (public, no auth). */
  submitResponse(dto: SubmitSurveyResponseDto): Observable<SurveySubmissionResultDto | { errors?: string[] }> {
    return this.http
      .post<SurveySubmissionResultDto & { errors?: string[] }>(`${this.apiUrl}/api/responses`, dto)
      .pipe(
        catchError((err) => {
          const body = err?.error;
          const errors = body?.errors ?? (body?.message ? [body.message] : ['Submission failed.']);
          return of({ errors: Array.isArray(errors) ? errors : [String(errors)] });
        })
      );
  }
}
