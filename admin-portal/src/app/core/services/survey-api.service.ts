import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, map } from 'rxjs';
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
  UpdateQuestionDto
} from '../api/survey-api.model';

@Injectable({ providedIn: 'root' })
export class SurveyApiService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getSurveys(): Observable<SurveyDto[]> {
    return this.http.get<SurveyDto[]>(`${this.apiUrl}/api/surveys`).pipe(
      catchError(() => of([]))
    );
  }

  getSurvey(id: number): Observable<SurveyDto | null> {
    return this.http.get<SurveyDto>(`${this.apiUrl}/api/surveys/${id}`).pipe(
      catchError(() => of(null))
    );
  }

  createSurvey(dto: CreateSurveyDto): Observable<SurveyDto | null> {
    return this.http.post<SurveyDto>(`${this.apiUrl}/api/surveys`, dto).pipe(
      catchError(() => of(null))
    );
  }

  updateSurvey(id: number, dto: UpdateSurveyDto): Observable<SurveyDto | null> {
    return this.http.put<SurveyDto>(`${this.apiUrl}/api/surveys/${id}`, dto).pipe(
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
    return this.http.get<QuestionDto[]>(`${this.apiUrl}/api/surveys/${surveyId}/questions`).pipe(
      catchError(() => of([]))
    );
  }

  getResponses(surveyId: number): Observable<SurveyResponseListItemDto[]> {
    return this.http
      .get<SurveyResponseListItemDto[]>(`${this.apiUrl}/api/surveys/${surveyId}/responses`)
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
}
