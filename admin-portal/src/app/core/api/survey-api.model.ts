/** Matches Survey API DTOs (camelCase from .NET API) */

export interface SurveyDto {
  id: number;
  title: string;
  description?: string;
  researcherId: number;
  createdAt: string;
}

export interface CreateSurveyDto {
  title: string;
  description?: string;
}

export interface UpdateSurveyDto {
  title: string;
  description?: string;
}

export enum QuestionType {
  Text = 0,
  TextArea = 1,
  Number = 2,
  YesNo = 3,
  SingleChoice = 4,
  MultipleChoice = 5,
  Rating = 6,
  Date = 7
}

export interface QuestionDto {
  id: number;
  surveyId: number;
  pageId: number;
  text: string;
  type: QuestionType;
  isRequired: boolean;
  order: number;
  optionsJson?: string;
}

export interface SurveyResponseListItemDto {
  id: number;
  surveyId: number;
  participantName?: string;
  submittedAt: string;
}

export interface QuestionSummaryDto {
  questionId: number;
  questionText: string;
  questionType: number;
  totalAnswers: number;
  summary: unknown;
}

export interface SurveySummaryDto {
  surveyId: number;
  surveyTitle: string;
  totalResponses: number;
  generatedAt: string;
  questions: QuestionSummaryDto[];
}

// Survey pages (README_API)
export interface SurveyPageDto {
  id: number;
  surveyId: number;
  title?: string;
  description?: string;
  order: number;
  questions: QuestionDto[];
}

export interface CreateSurveyPageDto {
  surveyId: number;
  title?: string;
  description?: string;
  order: number;
}

export interface UpdateSurveyPageDto {
  title?: string;
  description?: string;
  order: number;
}

// Questions create/update (README_API)
export interface CreateQuestionDto {
  surveyId: number;
  pageId?: number;
  text: string;
  type: QuestionType;
  isRequired: boolean;
  order: number;
  optionsJson?: string;
}

export interface UpdateQuestionDto {
  text: string;
  type: QuestionType;
  isRequired: boolean;
  pageId?: number;
  order: number;
  optionsJson?: string;
}
