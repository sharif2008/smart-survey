import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, map, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { DashboardStatsDto, ResearcherSurveySummaryDto } from '../api/dashboard-api.model';

function toResearcherSummary(raw: Record<string, unknown>): ResearcherSurveySummaryDto {
  return {
    surveyId: Number((raw['surveyId'] ?? raw['SurveyId']) ?? 0),
    title: String(raw['title'] ?? raw['Title'] ?? ''),
    responseCount: Number((raw['responseCount'] ?? raw['ResponseCount']) ?? 0)
  };
}

function toDashboardStats(raw: Record<string, unknown>): DashboardStatsDto {
  const summariesRaw = raw['surveySummaries'] ?? raw['SurveySummaries'];
  const list = Array.isArray(summariesRaw)
    ? (summariesRaw as Record<string, unknown>[]).map(toResearcherSummary)
    : [];
  const hourlyRaw = raw['hourlyResponses'] ?? raw['HourlyResponses'];
  const hourly =
    Array.isArray(hourlyRaw) && hourlyRaw.length
      ? (hourlyRaw as unknown[]).map((v) => Number(v ?? 0))
      : [];
  return {
    totalSurveys: Number((raw['totalSurveys'] ?? raw['TotalSurveys']) ?? 0),
    researcherCount: Number((raw['researcherCount'] ?? raw['ResearcherCount']) ?? 0),
    surveySummaries: list,
    hourlyResponses: hourly
  };
}

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getStats(): Observable<DashboardStatsDto> {
    return this.http.get<unknown>(`${this.apiUrl}/api/dashboard/stats`).pipe(
      map((body) =>
        body && typeof body === 'object'
          ? toDashboardStats(body as Record<string, unknown>)
          : { totalSurveys: 0, researcherCount: 0, surveySummaries: [], hourlyResponses: [] }
      ),
      catchError((err) => throwError(() => err))
    );
  }
}
