import { QuestionType } from '../api/survey-api.model';

/**
 * Registry of question type to component responsibility.
 * Used for schema-driven survey rendering (preview, participant form).
 * Each type is rendered by a dedicated or shared component that handles display + answer binding.
 */
export const QUESTION_TYPE_REGISTRY: Record<QuestionType, string> = {
  [QuestionType.Text]: 'TextQuestionComponent',
  [QuestionType.TextArea]: 'TextAreaQuestionComponent',
  [QuestionType.Number]: 'NumberQuestionComponent',
  [QuestionType.YesNo]: 'YesNoQuestionComponent',
  [QuestionType.SingleChoice]: 'RadioQuestionComponent',
  [QuestionType.MultipleChoice]: 'CheckboxQuestionComponent',
  [QuestionType.Rating]: 'RatingQuestionComponent',
  [QuestionType.Date]: 'DatePickerQuestionComponent',
  [QuestionType.Like]: 'LikeQuestionComponent',
  [QuestionType.Ranking]: 'RankingQuestionComponent',
  [QuestionType.NetPromoterScore]: 'NetPromoterScoreQuestionComponent'
};

export const QUESTION_TYPE_LABELS: Record<QuestionType, string> = {
  [QuestionType.Text]: 'Text',
  [QuestionType.TextArea]: 'Text area',
  [QuestionType.Number]: 'Number',
  [QuestionType.YesNo]: 'Yes/No',
  [QuestionType.SingleChoice]: 'Single choice',
  [QuestionType.MultipleChoice]: 'Multiple choice',
  [QuestionType.Rating]: 'Rating',
  [QuestionType.Date]: 'Date',
  [QuestionType.Like]: 'Like',
  [QuestionType.Ranking]: 'Ranking',
  [QuestionType.NetPromoterScore]: 'Net Promoter Score'
};
