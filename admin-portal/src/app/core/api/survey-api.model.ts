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
  Date = 7,
  Like = 8,
  Ranking = 9,
  NetPromoterScore = 10
}

/** Conditional display: show question when answer to another question matches. */
export interface ShowIfDto {
  questionId: number;
  operator: string;
  value?: string;
}

/** Validation rules; only set properties apply. */
export interface ValidationDto {
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  regex?: string;
  minNumber?: number;
  maxNumber?: number;
  dateMin?: string;
  dateMax?: string;
  optionMustExist?: boolean;
  maxSelectionCount?: number;
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
  showIf?: ShowIfDto;
  validation?: ValidationDto;
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
  /** Number of responses considered active (currently equals totalResponses). */
  activeResponses: number;
  /** Average time in seconds from survey creation until response submission. */
  averageCompletionSeconds: number;
  /** Duration in whole days between first and latest response. */
  durationDays: number;
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
  showIf?: ShowIfDto;
  validation?: ValidationDto;
}

export interface UpdateQuestionDto {
  text: string;
  type: QuestionType;
  isRequired: boolean;
  pageId?: number;
  order: number;
  optionsJson?: string;
  showIf?: ShowIfDto;
  validation?: ValidationDto;
}

/** Public survey submission (no auth). */
export interface SubmitAnswerDto {
  questionId: number;
  responseText?: string | null;
}

export interface SubmitSurveyResponseDto {
  surveyId: number;
  participantName?: string | null;
  answers: SubmitAnswerDto[];
}

export interface SurveySubmissionResultDto {
  surveyResponseId: number;
  surveyId: number;
  submittedAt: string;
}

export interface SurveyResponseAnswerDetailDto {
  questionId: number;
  questionText: string;
  questionType: QuestionType;
  responseText?: string | null;
}

export interface SurveyResponseDetailDto {
  id: number;
  surveyId: number;
  participantName?: string | null;
  submittedAt: string;
  /** Total time in seconds from survey creation until this response was submitted. */
  totalTimeSeconds: number;
  answers: SurveyResponseAnswerDetailDto[];
}
