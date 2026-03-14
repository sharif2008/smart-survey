import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CardComponent } from '../../../theme/shared/components/card/card.component';
import { SurveyApiService } from '../../../core/services/survey-api.service';
import {
  type SurveyDto,
  type QuestionDto,
  type SurveyResponseListItemDto,
  type SurveySummaryDto,
  type SurveyPageDto,
  type CreateQuestionDto,
  type UpdateQuestionDto,
  QuestionType
} from '../../../core/api/survey-api.model';

const QUESTION_TYPES: Record<QuestionType, string> = {
  [QuestionType.Text]: 'Text',
  [QuestionType.TextArea]: 'TextArea',
  [QuestionType.Number]: 'Number',
  [QuestionType.YesNo]: 'Yes/No',
  [QuestionType.SingleChoice]: 'Single choice',
  [QuestionType.MultipleChoice]: 'Multiple choice',
  [QuestionType.Rating]: 'Rating',
  [QuestionType.Date]: 'Date'
};

@Component({
  selector: 'app-survey-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, CardComponent],
  templateUrl: './survey-detail.component.html',
  styleUrls: ['./survey-detail.component.scss']
})
export class SurveyDetailComponent implements OnInit {
  surveyId = 0;
  survey: SurveyDto | null = null;
  questions: QuestionDto[] = [];
  pages: SurveyPageDto[] = [];
  responses: SurveyResponseListItemDto[] = [];
  summary: SurveySummaryDto | null = null;
  loading = true;
  error = '';
  activeTab: 'questions' | 'pages' | 'responses' | 'summary' = 'questions';
  questionTypeName = QUESTION_TYPES;
  questionTypes = [
    { value: QuestionType.Text, label: 'Text' },
    { value: QuestionType.TextArea, label: 'TextArea' },
    { value: QuestionType.Number, label: 'Number' },
    { value: QuestionType.YesNo, label: 'Yes/No' },
    { value: QuestionType.SingleChoice, label: 'Single choice' },
    { value: QuestionType.MultipleChoice, label: 'Multiple choice' },
    { value: QuestionType.Rating, label: 'Rating' },
    { value: QuestionType.Date, label: 'Date' }
  ];

  editSurveyModal = false;
  editTitle = '';
  editDescription = '';
  editSurveySaving = false;

  addPageModal = false;
  pageTitle = '';
  pageDescription = '';
  pageOrder = 0;
  addPageSaving = false;
  editPageModal: SurveyPageDto | null = null;
  editPageTitle = '';
  editPageDescription = '';
  editPageOrder = 0;
  editPageSaving = false;
  deletePageId: number | null = null;

  addQuestionModal = false;
  qText = '';
  qType: QuestionType = QuestionType.Text;
  qRequired = false;
  qOrder = 1;
  qPageId: number | null = null;
  qOptionsInput = ''; // one option per line for SingleChoice/MultipleChoice
  addQuestionSaving = false;
  editQuestionModal: QuestionDto | null = null;
  editQText = '';
  editQType: QuestionType = QuestionType.Text;
  editQRequired = false;
  editQOrder = 0;
  editQOptionsInput = '';
  editQuestionSaving = false;
  deleteQuestionId: number | null = null;
  expandedQuestionId: number | null = null;

  get showQOptions(): boolean {
    return this.qType === QuestionType.SingleChoice || this.qType === QuestionType.MultipleChoice;
  }
  get showEditQOptions(): boolean {
    return this.editQType === QuestionType.SingleChoice || this.editQType === QuestionType.MultipleChoice;
  }

  constructor(
    private route: ActivatedRoute,
    private api: SurveyApiService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.surveyId = id ? parseInt(id, 10) : 0;
    if (!this.surveyId) {
      this.error = 'Invalid survey id.';
      this.loading = false;
      return;
    }
    this.loadSurvey(this.surveyId);
  }

  loadSurvey(sid: number): void {
    this.loading = true;
    this.error = '';
    this.api.getSurvey(sid).subscribe({
      next: (s) => {
        this.survey = s;
        if (!s) {
          this.error = 'Survey not found.';
          this.loading = false;
          return;
        }
        this.loading = false;
        this.refreshData();
      },
      error: () => {
        this.error = 'Failed to load survey.';
        this.loading = false;
      }
    });
  }

  refreshData(): void {
    if (!this.surveyId) return;
    this.api.getQuestions(this.surveyId).subscribe((q) => (this.questions = q));
    this.api.getPages(this.surveyId).subscribe((p) => (this.pages = p));
    this.api.getResponses(this.surveyId).subscribe((r) => (this.responses = r));
    this.api.getSummary(this.surveyId).subscribe((sum) => (this.summary = sum));
  }

  openEditSurvey(): void {
    if (!this.survey) return;
    this.editTitle = this.survey.title;
    this.editDescription = this.survey.description ?? '';
    this.editSurveyModal = true;
  }

  closeEditSurvey(): void {
    this.editSurveyModal = false;
  }

  saveEditSurvey(): void {
    if (!this.survey) return;
    const title = this.editTitle?.trim();
    if (!title) return;
    this.editSurveySaving = true;
    this.api.updateSurvey(this.survey.id, { title, description: this.editDescription?.trim() || undefined }).subscribe({
      next: (updated) => {
        this.editSurveySaving = false;
        this.closeEditSurvey();
        if (updated) {
          this.survey = updated;
          this.refreshData();
        }
      },
      error: () => {
        this.editSurveySaving = false;
      }
    });
  }

  openAddPage(): void {
    this.pageTitle = '';
    this.pageDescription = '';
    this.pageOrder = this.pages.length;
    this.addPageModal = true;
  }

  closeAddPage(): void {
    this.addPageModal = false;
  }

  saveAddPage(): void {
    this.addPageSaving = true;
    this.api.createPage(this.surveyId, { title: this.pageTitle?.trim() || undefined, description: this.pageDescription?.trim() || undefined, order: this.pageOrder }).subscribe({
      next: (p) => {
        this.addPageSaving = false;
        this.closeAddPage();
        if (p) this.refreshData();
      },
      error: () => {
        this.addPageSaving = false;
      }
    });
  }

  openEditPage(p: SurveyPageDto): void {
    this.editPageModal = p;
    this.editPageTitle = p.title ?? '';
    this.editPageDescription = p.description ?? '';
    this.editPageOrder = p.order;
  }

  closeEditPage(): void {
    this.editPageModal = null;
  }

  saveEditPage(): void {
    if (!this.editPageModal) return;
    this.editPageSaving = true;
    this.api.updatePage(this.surveyId, this.editPageModal.id, { title: this.editPageTitle?.trim() || undefined, description: this.editPageDescription?.trim() || undefined, order: this.editPageOrder }).subscribe({
      next: (p) => {
        this.editPageSaving = false;
        this.closeEditPage();
        if (p) this.refreshData();
      },
      error: () => {
        this.editPageSaving = false;
      }
    });
  }

  confirmDeletePage(pageId: number): void {
    this.deletePageId = pageId;
  }

  cancelDeletePage(): void {
    this.deletePageId = null;
  }

  deletePage(pageId: number): void {
    this.api.deletePage(this.surveyId, pageId).subscribe({
      next: (ok) => {
        if (ok) {
          this.deletePageId = null;
          this.refreshData();
        }
      }
    });
  }

  openAddQuestion(): void {
    this.qText = '';
    this.qType = QuestionType.Text;
    this.qRequired = false;
    this.qOrder = this.questions.length + 1;
    this.qPageId = this.pages.length > 0 ? this.pages[0].id : null;
    this.qOptionsInput = '';
    this.addQuestionModal = true;
    this.activeTab = 'questions';
  }

  closeAddQuestion(): void {
    this.addQuestionModal = false;
  }

  saveAddQuestion(): void {
    const text = this.qText?.trim();
    if (!text) return;
    this.addQuestionSaving = true;
    const optionsJson = this.buildOptionsJson(this.qOptionsInput);
    const dto: CreateQuestionDto = {
      surveyId: this.surveyId,
      pageId: this.qPageId ?? undefined,
      text,
      type: this.qType,
      isRequired: this.qRequired,
      order: this.qOrder,
      optionsJson: optionsJson ?? undefined
    };
    this.api.createQuestion(dto).subscribe({
      next: (q) => {
        this.addQuestionSaving = false;
        this.closeAddQuestion();
        if (q) this.refreshData();
      },
      error: () => {
        this.addQuestionSaving = false;
      }
    });
  }

  openEditQuestion(q: QuestionDto): void {
    this.editQuestionModal = q;
    this.editQText = q.text;
    this.editQType = q.type;
    this.editQRequired = q.isRequired;
    this.editQOrder = q.order;
    this.editQOptionsInput = this.parseOptionsToLines(q.optionsJson);
  }

  private buildOptionsJson(input: string): string | null {
    const lines = (input || '')
      .split(/[\r\n]+/)
      .map((s) => s.trim())
      .filter(Boolean);
    if (lines.length === 0) return null;
    return JSON.stringify(lines);
  }

  private parseOptionsToLines(optionsJson?: string | null): string {
    if (!optionsJson) return '';
    try {
      const arr = JSON.parse(optionsJson) as string[];
      return Array.isArray(arr) ? arr.join('\n') : '';
    } catch {
      return '';
    }
  }

  getQuestionOptions(q: QuestionDto): string[] {
    if (!q.optionsJson) return [];
    try {
      const arr = JSON.parse(q.optionsJson) as string[];
      return Array.isArray(arr) ? arr : [];
    } catch {
      return [];
    }
  }

  isChoiceQuestion(q: QuestionDto): boolean {
    return q.type === QuestionType.SingleChoice || q.type === QuestionType.MultipleChoice;
  }

  toggleQuestionExpanded(qId: number): void {
    this.expandedQuestionId = this.expandedQuestionId === qId ? null : qId;
  }

  closeEditQuestion(): void {
    this.editQuestionModal = null;
  }

  saveEditQuestion(): void {
    if (!this.editQuestionModal) return;
    const text = this.editQText?.trim();
    if (!text) return;
    this.editQuestionSaving = true;
    const optionsJson = this.buildOptionsJson(this.editQOptionsInput);
    this.api.updateQuestion(this.editQuestionModal.id, {
      text,
      type: this.editQType,
      isRequired: this.editQRequired,
      order: this.editQOrder,
      optionsJson: optionsJson ?? undefined
    }).subscribe({
      next: (q) => {
        this.editQuestionSaving = false;
        this.closeEditQuestion();
        if (q) this.refreshData();
      },
      error: () => {
        this.editQuestionSaving = false;
      }
    });
  }

  confirmDeleteQuestion(id: number): void {
    this.deleteQuestionId = id;
  }

  cancelDeleteQuestion(): void {
    this.deleteQuestionId = null;
  }

  deleteQuestion(id: number): void {
    this.api.deleteQuestion(id).subscribe({
      next: (ok) => {
        if (ok) {
          this.deleteQuestionId = null;
          this.refreshData();
        }
      }
    });
  }

  formatDate(s: string): string {
    if (!s) return '—';
    try {
      return new Date(s).toLocaleString();
    } catch {
      return s;
    }
  }
}
