import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { CardComponent } from '../../../theme/shared/components/card/card.component';
import { SurveyApiService } from '../../../core/services/survey-api.service';
import { SurveyQuestionRendererComponent } from '../../../core/survey/survey-question-renderer.component';
import {
  type SurveyDto,
  type QuestionDto,
  type SurveyResponseListItemDto,
  type SurveySummaryDto,
  type SurveyPageDto,
  type CreateQuestionDto,
  type UpdateQuestionDto,
  type ShowIfDto,
  type ValidationDto,
  QuestionType,
  type SurveyResponseDetailDto
} from '../../../core/api/survey-api.model';
import { NgApexchartsModule, type ApexOptions } from 'ng-apexcharts';

const QUESTION_TYPES: Record<QuestionType, string> = {
  [QuestionType.Text]: 'Text',
  [QuestionType.TextArea]: 'TextArea',
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

@Component({
  selector: 'app-survey-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, CardComponent, SurveyQuestionRendererComponent, NgApexchartsModule],
  templateUrl: './survey-detail.component.html',
  styleUrls: ['./survey-detail.component.scss']
})
export class SurveyDetailComponent implements OnInit {
  surveyId = 0;
  survey: SurveyDto | null = null;
  questions: QuestionDto[] = [];
  pages: SurveyPageDto[] = [];
  responses: SurveyResponseListItemDto[] = [];
  responseDetails: SurveyResponseDetailDto[] = [];
  summary: SurveySummaryDto | null = null;
  loading = true;
  error = '';
  activeTab: 'builder' | 'questions' | 'pages' | 'responses' | 'summary' | 'analytics' = 'builder';
  questionTypeName = QUESTION_TYPES;
  questionTypes = [
    { value: QuestionType.Text, label: 'Text' },
    { value: QuestionType.TextArea, label: 'TextArea' },
    { value: QuestionType.Number, label: 'Number' },
    { value: QuestionType.YesNo, label: 'Yes/No' },
    { value: QuestionType.SingleChoice, label: 'Single choice' },
    { value: QuestionType.MultipleChoice, label: 'Multiple choice' },
    { value: QuestionType.Rating, label: 'Rating' },
    { value: QuestionType.Date, label: 'Date' },
    { value: QuestionType.Like, label: 'Like' },
    { value: QuestionType.Ranking, label: 'Ranking' },
    { value: QuestionType.NetPromoterScore, label: 'Net Promoter Score' }
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
  editQPageId: number | null = null;
  editQOptionsInput = '';
  editQShowIfQuestionId: number | null = null;
  editQShowIfOperator = 'equals';
  editQShowIfValue = '';
  editQValMinLength: number | null = null;
  editQValMaxLength: number | null = null;
  editQValRegex = '';
  editQValMinNumber: number | null = null;
  editQValMaxNumber: number | null = null;
  editQuestionSaving = false;
  deleteQuestionId: number | null = null;
  expandedQuestionId: number | null = null;
  previewMode = false;
  previewAnswers: Record<number, string | number | string[]> = {};
  shareCopied = false;
  responseDetailModal: SurveyResponseDetailDto | null = null;
  questionResponsesModal: { questionId: number; questionText: string; items: { participantName: string; submittedAt: string; answerText: string }[] } | null = null;

  // --- Analytics helpers ---
  get hasAnalytics(): boolean {
    return !!this.summary && this.summary.questions?.length > 0;
  }

  getChartOptionsForQuestion(qs: SurveySummaryDto['questions'][number]): Partial<ApexOptions> | null {
    if (!qs?.summary) return null;
    const type = qs.questionType as QuestionType;
    const s = qs.summary as any;

    if (type === QuestionType.YesNo) {
      const yes = Number(s.yesCount ?? 0);
      const no = Number(s.noCount ?? 0);
      if (!yes && !no) return null;
      return {
        chart: { type: 'pie', height: 260, toolbar: { show: false }, background: 'transparent' },
        labels: ['Yes', 'No'],
        series: [yes, no],
        legend: { position: 'bottom' }
      };
    }

    if (type === QuestionType.Like) {
      const like = Number(s.likeCount ?? 0);
      const dislike = Number(s.dislikeCount ?? 0);
      if (!like && !dislike) return null;
      return {
        chart: { type: 'pie', height: 260, toolbar: { show: false }, background: 'transparent' },
        labels: ['Like', 'Dislike'],
        series: [like, dislike],
        legend: { position: 'bottom' }
      };
    }

    if (type === QuestionType.SingleChoice || type === QuestionType.MultipleChoice || type === QuestionType.Ranking) {
      const options = Array.isArray(s.options) ? s.options : [];
      if (!options.length) return null;
      const labels = options.map((o: any) => String(o.label ?? o.Label ?? ''));
      const counts = options.map((o: any) => Number(o.count ?? o.Count ?? 0));
      return {
        chart: { type: 'bar', height: 260, toolbar: { show: false }, background: 'transparent' },
        series: [{ name: 'Responses', data: counts }],
        xaxis: { categories: labels },
        dataLabels: { enabled: false },
        plotOptions: { bar: { borderRadius: 4, columnWidth: '45%' } }
      };
    }

    if (type === QuestionType.Rating || type === QuestionType.NetPromoterScore || type === QuestionType.Number) {
      const distribution = Array.isArray(s.distribution) ? s.distribution : [];
      if (!distribution.length) return null;
      const labels = distribution.map((d: any) => String(d.label ?? d.Label ?? ''));
      const counts = distribution.map((d: any) => Number(d.count ?? d.Count ?? 0));
      return {
        chart: { type: 'bar', height: 260, toolbar: { show: false }, background: 'transparent' },
        series: [{ name: 'Responses', data: counts }],
        xaxis: { categories: labels },
        dataLabels: { enabled: false },
        plotOptions: { bar: { borderRadius: 4, columnWidth: '45%' } }
      };
    }

    if (type === QuestionType.Date) {
      const byDay = Array.isArray(s.groupedByDay) ? s.groupedByDay : [];
      if (!byDay.length) return null;
      const labels = byDay.map((d: any) => String(d.date ?? d.Date ?? ''));
      const counts = byDay.map((d: any) => Number(d.count ?? d.Count ?? 0));
      return {
        chart: { type: 'line', height: 260, toolbar: { show: false }, background: 'transparent' },
        series: [{ name: 'Responses', data: counts }],
        xaxis: { categories: labels },
        dataLabels: { enabled: false },
        stroke: { curve: 'smooth', width: 2 }
      };
    }

    return null;
  }

  getSummaryMetaText(qs: SurveySummaryDto['questions'][number]): string | null {
    const type = qs.questionType as QuestionType;
    const s = qs.summary as any;

    if (type === QuestionType.YesNo) {
      if (!s) return null;
      return `Yes ${s.yesPercentage ?? 0}% · No ${s.noPercentage ?? 0}%`;
    }
    if (type === QuestionType.Like) {
      if (!s) return null;
      return `Like ${s.likePercentage ?? 0}% · Dislike ${s.dislikePercentage ?? 0}%`;
    }
    if (type === QuestionType.Rating || type === QuestionType.Number) {
      if (!s) return null;
      return `Avg ${s.average ?? 0} · Min ${s.min ?? 0} · Max ${s.max ?? 0}`;
    }
    if (type === QuestionType.NetPromoterScore) {
      if (!s) return null;
      return `NPS ${s.npsScore ?? 0} · Detractors ${s.detractors ?? 0} · Passives ${s.passives ?? 0} · Promoters ${s.promoters ?? 0}`;
    }
    if (type === QuestionType.Date) {
      if (!s) return null;
      if (!s.earliest || !s.latest) return null;
      return `Range ${s.earliest} → ${s.latest}`;
    }
    return null;
  }

  get showQOptions(): boolean {
    return this.qType === QuestionType.SingleChoice || this.qType === QuestionType.MultipleChoice || this.qType === QuestionType.Ranking;
  }
  get showEditQOptions(): boolean {
    return this.editQType === QuestionType.SingleChoice || this.editQType === QuestionType.MultipleChoice || this.editQType === QuestionType.Ranking;
  }

  constructor(
    private route: ActivatedRoute,
    private api: SurveyApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      this.surveyId = id ? parseInt(id, 10) : 0;
      if (!this.surveyId) {
        this.error = 'Invalid survey id.';
        this.loading = false;
        this.cdr.detectChanges();
        return;
      }
      this.loadSurvey(this.surveyId);
    });
  }

  loadSurvey(sid: number): void {
    this.loading = true;
    this.error = '';
    this.survey = null;
    this.questions = [];
    this.pages = [];
    this.cdr.detectChanges();
    this.api.getSurvey(sid).subscribe({
      next: (s) => {
        this.survey = s;
        if (!s) {
          this.error = 'Survey not found.';
          this.loading = false;
          this.cdr.detectChanges();
          return;
        }
        this.loading = false;
        this.refreshData();
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load survey.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  refreshData(): void {
    if (!this.surveyId) return;
    forkJoin({
      questions: this.api.getQuestions(this.surveyId),
      pages: this.api.getPages(this.surveyId),
      responses: this.api.getResponses(this.surveyId),
      responseDetails: this.api.getResponseDetails(this.surveyId),
      summary: this.api.getSummary(this.surveyId)
    }).subscribe(({ questions, pages, responses, responseDetails, summary }) => {
      this.questions = questions;
      this.pages = pages;
      this.responses = responses;
      this.responseDetails = responseDetails;
      this.summary = summary;
      this.cdr.detectChanges();
      // Force extra passes so canvas question text paints without needing a click
      setTimeout(() => this.cdr.detectChanges(), 0);
      setTimeout(() => this.cdr.detectChanges(), 50);
      setTimeout(() => this.cdr.detectChanges(), 150);
    });
  }

  openResponseDetails(id: number): void {
    const detail = this.responseDetails.find((r) => r.id === id);
    if (detail) {
      this.responseDetailModal = detail;
      this.cdr.detectChanges();
      return;
    }
    // If details weren't loaded (e.g. 404 before), fetch on demand
    this.api.getResponseDetails(this.surveyId).subscribe((list) => {
      this.responseDetails = list;
      const d = list.find((r) => r.id === id);
      if (d) {
        this.responseDetailModal = d;
      }
      this.cdr.detectChanges();
    });
  }

  closeResponseDetails(): void {
    this.responseDetailModal = null;
  }

  openQuestionResponses(questionId: number, questionText: string): void {
    const getAnswers = (r: SurveyResponseDetailDto | Record<string, unknown>): unknown[] =>
      Array.isArray((r as any).answers) ? (r as any).answers : Array.isArray((r as any).Answers) ? (r as any).Answers : [];
    const buildItems = (details: (SurveyResponseDetailDto | Record<string, unknown>)[]): { participantName: string; submittedAt: string; answerText: string }[] => {
      const items: { participantName: string; submittedAt: string; answerText: string }[] = [];
      const qId = Number(questionId);
      for (const r of details) {
        const rawAnswers = getAnswers(r);
        const a = rawAnswers.find((x: Record<string, unknown>) =>
          Number(x?.questionId ?? x?.QuestionId ?? 0) === qId
        ) as Record<string, unknown> | undefined;
        if (!a) continue;
        const participantName = (r as any).participantName ?? (r as any).ParticipantName ?? '—';
        const submittedAt = (r as any).submittedAt ?? (r as any).SubmittedAt ?? '';
        const answerText = a?.responseText ?? a?.ResponseText ?? '—';
        items.push({
          participantName: participantName != null ? String(participantName) : '—',
          submittedAt: String(submittedAt),
          answerText: answerText != null ? String(answerText) : '—'
        });
      }
      return items;
    };

    if (this.responseDetails.length === 0) {
      this.api.getResponseDetails(this.surveyId).subscribe((list) => {
        const details = Array.isArray(list) ? list : [];
        this.responseDetails = details as SurveyResponseDetailDto[];
        const items = buildItems(details as (SurveyResponseDetailDto | Record<string, unknown>)[]);
        this.questionResponsesModal = { questionId, questionText, items };
        this.cdr.detectChanges();
      });
    } else {
      const items = buildItems(this.responseDetails as (SurveyResponseDetailDto | Record<string, unknown>)[]);
      this.questionResponsesModal = { questionId, questionText, items };
    }
  }

  closeQuestionResponses(): void {
    this.questionResponsesModal = null;
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
    this.openAddQuestionWithType(QuestionType.Text);
  }

  openAddQuestionWithType(type: QuestionType): void {
    this.qText = '';
    this.qType = type;
    this.qRequired = false;
    this.qOrder = this.questions.length + 1;
    this.qPageId = this.pages.length > 0 ? this.pages[0].id : null;
    this.qOptionsInput = '';
    this.addQuestionModal = true;
    this.activeTab = 'builder';
  }

  closeAddQuestion(): void {
    this.addQuestionModal = false;
  }

  saveAddQuestion(): void {
    const text = this.qText?.trim();
    if (!text) return;
    this.addQuestionSaving = true;
    const optionsJson = this.buildOptionsJson(this.qOptionsInput);
    const type = typeof this.qType === 'number' ? this.qType : Number(this.qType);
    const dto: CreateQuestionDto = {
      surveyId: this.surveyId,
      pageId: this.qPageId ?? undefined,
      text,
      type,
      isRequired: this.qRequired,
      order: this.qOrder,
      optionsJson: optionsJson ?? undefined,
      showIf: undefined,
      validation: undefined
    };
    this.api.createQuestion(dto).subscribe({
      next: (q) => {
        this.addQuestionSaving = false;
        this.closeAddQuestion();
        if (q) this.refreshData();
        this.cdr.detectChanges();
      },
      error: () => {
        this.addQuestionSaving = false;
        this.cdr.detectChanges();
      }
    });
  }

  openEditQuestion(q: QuestionDto): void {
    this.editQuestionModal = q;
    this.editQText = q.text;
    this.editQType = q.type;
    this.editQRequired = q.isRequired;
    this.editQOrder = q.order;
    this.editQPageId = q.pageId ?? null;
    this.editQOptionsInput = this.parseOptionsToLines(q.optionsJson);
    this.editQShowIfQuestionId = q.showIf?.questionId ?? null;
    this.editQShowIfOperator = q.showIf?.operator ?? 'equals';
    this.editQShowIfValue = q.showIf?.value ?? '';
    this.editQValMinLength = q.validation?.minLength ?? null;
    this.editQValMaxLength = q.validation?.maxLength ?? null;
    this.editQValRegex = q.validation?.regex ?? '';
    this.editQValMinNumber = q.validation?.minNumber ?? null;
    this.editQValMaxNumber = q.validation?.maxNumber ?? null;
    this.cdr.detectChanges();
  }

  clearSelectedQuestion(): void {
    this.editQuestionModal = null;
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
    return q.type === QuestionType.SingleChoice || q.type === QuestionType.MultipleChoice || q.type === QuestionType.Ranking;
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
    this.cdr.detectChanges();
    const optionsJson = this.buildOptionsJson(this.editQOptionsInput);
    const showIf: ShowIfDto | undefined =
      this.editQShowIfQuestionId != null
        ? { questionId: this.editQShowIfQuestionId, operator: this.editQShowIfOperator, value: this.editQShowIfValue || undefined }
        : undefined;
    const validation: ValidationDto = {};
    if (this.editQValMinLength != null) validation.minLength = this.editQValMinLength;
    if (this.editQValMaxLength != null) validation.maxLength = this.editQValMaxLength;
    if (this.editQValRegex?.trim()) validation.regex = this.editQValRegex.trim();
    if (this.editQValMinNumber != null) validation.minNumber = this.editQValMinNumber;
    if (this.editQValMaxNumber != null) validation.maxNumber = this.editQValMaxNumber;
    const type = typeof this.editQType === 'number' ? this.editQType : Number(this.editQType);
    this.api.updateQuestion(this.editQuestionModal.id, {
      text,
      type,
      isRequired: this.editQRequired,
      pageId: this.editQPageId ?? undefined,
      order: this.editQOrder,
      optionsJson: optionsJson ?? undefined,
      showIf,
      validation: Object.keys(validation).length > 0 ? validation : undefined
    }).subscribe({
      next: (q) => {
        this.editQuestionSaving = false;
        this.closeEditQuestion();
        if (q) this.refreshData();
        this.cdr.detectChanges();
      },
      error: () => {
        this.editQuestionSaving = false;
        this.cdr.detectChanges();
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
          if (this.editQuestionModal?.id === id) this.clearSelectedQuestion();
          this.refreshData();
        }
      }
    });
  }

  getQuestionsForPage(pageId: number): QuestionDto[] {
    return this.questions.filter((q) => q.pageId === pageId).sort((a, b) => a.order - b.order);
  }

  duplicateQuestion(q: QuestionDto): void {
    const optionsJson = q.optionsJson ?? undefined;
    const dto: CreateQuestionDto = {
      surveyId: this.surveyId,
      pageId: q.pageId,
      text: q.text + ' (copy)',
      type: q.type,
      isRequired: q.isRequired,
      order: q.order + 1,
      optionsJson,
      showIf: q.showIf,
      validation: q.validation
    };
    this.api.createQuestion(dto).subscribe({
      next: (created) => {
        if (created) this.refreshData();
      }
    });
  }

  setPreviewAnswer(questionId: number, value: string | number | string[]): void {
    this.previewAnswers = { ...this.previewAnswers, [questionId]: value };
  }

  moveQuestionOrder(q: QuestionDto, delta: number): void {
    const pageQuestions = this.getQuestionsForPage(q.pageId);
    const idx = pageQuestions.findIndex((x) => x.id === q.id);
    if (idx < 0 || (delta < 0 && idx === 0) || (delta > 0 && idx === pageQuestions.length - 1)) return;
    const swap = pageQuestions[idx + delta];
    if (!swap) return;
    this.api.updateQuestion(q.id, { text: q.text, type: q.type, isRequired: q.isRequired, order: swap.order, optionsJson: q.optionsJson, showIf: q.showIf, validation: q.validation }).subscribe({
      next: () => {
        this.api.updateQuestion(swap.id, { text: swap.text, type: swap.type, isRequired: swap.isRequired, order: q.order, optionsJson: swap.optionsJson, showIf: swap.showIf, validation: swap.validation }).subscribe({
          next: () => this.refreshData()
        });
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

  formatDurationSeconds(seconds: number | null | undefined): string {
    if (!seconds || seconds <= 0) return '00:00';
    const total = Math.round(seconds);
    const mins = Math.floor(total / 60);
    const secs = total % 60;
    const mm = mins.toString().padStart(2, '0');
    const ss = secs.toString().padStart(2, '0');
    return `${mm}:${ss}`;
  }

  /** Public survey URL (no auth) for sharing. */
  get publicSurveyUrl(): string {
    if (typeof window === 'undefined' || !this.surveyId) return '';
    const baseHref = document.querySelector('base')?.getAttribute('href') || '/';
    const baseUrl = baseHref.startsWith('http') ? baseHref : (window.location.origin + (baseHref.startsWith('/') ? baseHref : '/' + baseHref));
    return (baseUrl.replace(/\/?$/, '') || baseUrl) + '/s/' + this.surveyId;
  }

  get publicSurveyQrSrc(): string {
    const url = this.publicSurveyUrl;
    if (!url) return '';
    return 'https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=' + encodeURIComponent(url);
  }

  copyPublicUrl(): void {
    const url = this.publicSurveyUrl;
    if (!url) return;
    navigator.clipboard?.writeText(url).then(() => {
      this.shareCopied = true;
      this.cdr.detectChanges();
      setTimeout(() => {
        this.shareCopied = false;
        this.cdr.detectChanges();
      }, 2000);
    });
  }

  shareByEmail(): void {
    const url = this.publicSurveyUrl;
    const subject = this.survey ? encodeURIComponent('Survey: ' + this.survey.title) : '';
    const body = url ? encodeURIComponent('Please take this survey:\n' + url) : '';
    window.location.href = 'mailto:?subject=' + subject + '&body=' + body;
  }
}
