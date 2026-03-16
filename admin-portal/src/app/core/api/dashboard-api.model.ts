/** Dashboard stats from API (camelCase or PascalCase). */

export interface ResearcherSurveySummaryDto {
  surveyId: number;
  title: string;
  responseCount: number;
}

export interface DashboardStatsDto {
  totalSurveys: number;
  researcherCount: number;
  surveySummaries: ResearcherSurveySummaryDto[];
  hourlyResponses: number[];
}
