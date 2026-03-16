import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { SurveyApiService } from '../../core/services/survey-api.service';
import { SurveyQuestionRendererComponent } from '../../core/survey/survey-question-renderer.component';
import type { SurveyDto, QuestionDto, SurveyPageDto } from '../../core/api/survey-api.model';

@Component({
  selector: 'app-public-survey',
  standalone: true,
  imports: [CommonModule, FormsModule, SurveyQuestionRendererComponent],
  templateUrl: './public-survey.component.html',
  styleUrls: ['./public-survey.component.scss']
})
export class PublicSurveyComponent implements OnInit {
  surveyId = 0;
  survey: SurveyDto | null = null;
  pages: SurveyPageDto[] = [];
  /** Flattened list of questions per page (order preserved). */
  pagesWithQuestions: { page: SurveyPageDto; questions: QuestionDto[] }[] = [];
  loading = true;
  error = '';
  currentPageIndex = 0;
  answers: Record<number, string | number | string[]> = {};
  submitted = false;
  submitting = false;
  submitError: string[] = [];

  private get submittedKey(): string {
    return `surveymind_submitted_${this.surveyId}`;
  }

  private get progressKey(): string {
    return `surveymind_progress_${this.surveyId}`;
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
        this.error = 'Invalid survey.';
        this.loading = false;
        this.cdr.detectChanges();
        return;
      }

      // If this browser has already completed the survey, show thank-you immediately.
      if (typeof window !== 'undefined') {
        this.submitted = window.localStorage.getItem(this.submittedKey) === '1';
      }

      this.loadSurvey();
    });
  }

  loadSurvey(): void {
    this.loading = true;
    this.error = '';
    this.survey = null;
    this.pages = [];
    this.pagesWithQuestions = [];
    this.cdr.detectChanges();

    // Step 1: Load survey only. If not active or expired, show error and do not load form (pages/questions).
    this.api.getSurveyPublic(this.surveyId).subscribe({
      next: (survey) => {
        if (!survey) {
          this.error = 'Survey not found.';
          this.loading = false;
          this.cdr.detectChanges();
          return;
        }
        if (survey.status !== 1) {
          this.error = 'This survey is not available.';
          this.loading = false;
          this.cdr.detectChanges();
          return;
        }
        if (survey.endsAt) {
          try {
            const end = new Date(survey.endsAt).getTime();
            if (Number.isFinite(end) && Date.now() >= end) {
              this.error = 'This survey has ended.';
              this.loading = false;
              this.cdr.detectChanges();
              return;
            }
          } catch {
            // ignore
          }
        }
        this.survey = survey;
        this.loadPagesAndQuestions();
      },
      error: (err: { message?: string }) => {
        this.error = err?.message ?? 'This survey is not available.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadPagesAndQuestions(): void {
    forkJoin({
      pages: this.api.getPagesPublic(this.surveyId),
      questions: this.api.getQuestionsPublic(this.surveyId)
    }).subscribe({
      next: ({ pages, questions }) => {
        this.pages = pages;
        const sortedPages = [...pages].sort((a, b) => a.order - b.order);
        this.pagesWithQuestions = sortedPages.map((page) => ({
          page,
          questions: questions
            .filter((q) => q.pageId === page.id)
            .sort((a, b) => a.order - b.order)
        }));
        this.loading = false;

        if (!this.submitted && typeof window !== 'undefined') {
          const raw = window.localStorage.getItem(this.progressKey);
          if (raw) {
            try {
              const parsed = JSON.parse(raw) as { pageIndex?: number; answers?: Record<string, string | number | string[]> };
              if (parsed.answers) {
                const restored: Record<number, string | number | string[]> = {};
                for (const [k, v] of Object.entries(parsed.answers)) {
                  const id = Number(k);
                  if (!Number.isNaN(id)) restored[id] = v as any;
                }
                this.answers = restored;
              }
              if (typeof parsed.pageIndex === 'number' && parsed.pageIndex >= 0 && parsed.pageIndex < this.pagesWithQuestions.length) {
                this.currentPageIndex = parsed.pageIndex;
              }
            } catch {
              // Ignore malformed progress
            }
          }
        }

        this.cdr.detectChanges();
      },
      error: (err: { message?: string }) => {
        this.error = err?.message ?? 'Failed to load survey.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get currentPageData(): { page: SurveyPageDto; questions: QuestionDto[] } | null {
    return this.pagesWithQuestions[this.currentPageIndex] ?? null;
  }

  get isLastPage(): boolean {
    return this.currentPageIndex >= this.pagesWithQuestions.length - 1;
  }

  get canGoNext(): boolean {
    const data = this.currentPageData;
    if (!data) return false;
    for (const q of data.questions) {
      if (!q.isRequired) continue;
      const v = this.answers[q.id];
      if (v === undefined || v === null || v === '' || (Array.isArray(v) && v.length === 0))
        return false;
    }
    return true;
  }

  setAnswer(questionId: number, value: string | number | string[]): void {
    this.answers = { ...this.answers, [questionId]: value };
    this.persistProgress();
  }

  next(): void {
    if (!this.canGoNext || this.isLastPage) return;
    this.currentPageIndex++;
    this.cdr.detectChanges();
  }

  previous(): void {
    if (this.currentPageIndex <= 0) return;
    this.currentPageIndex--;
    this.persistProgress();
    this.cdr.detectChanges();
  }

  submit(): void {
    if (!this.survey || this.submitting || this.submitted) return;
    const answers = this.buildSubmitAnswers();
    if (answers.length === 0) {
      this.submitError = ['Please answer at least one question.'];
      this.cdr.detectChanges();
      return;
    }
    this.submitting = true;
    this.submitError = [];
    this.cdr.detectChanges();

    this.api
      .submitResponse({
        surveyId: this.surveyId,
        participantName: undefined,
        answers
      })
      .subscribe({
        next: (result) => {
          this.submitting = false;
          if ('errors' in result && result.errors?.length) {
            this.submitError = result.errors;
            this.cdr.detectChanges();
            return;
          }
          this.submitted = true;
          if (typeof window !== 'undefined') {
            window.localStorage.setItem(this.submittedKey, '1');
            window.localStorage.removeItem(this.progressKey);
          }
          this.cdr.detectChanges();
        },
        error: () => {
          this.submitting = false;
          this.submitError = ['Unable to submit. Please try again.'];
          this.cdr.detectChanges();
        }
      });
  }

  private buildSubmitAnswers(): { questionId: number; responseText: string | null }[] {
    const out: { questionId: number; responseText: string | null }[] = [];
    for (const { questions } of this.pagesWithQuestions) {
      for (const q of questions) {
        const v = this.answers[q.id];
        if (v === undefined || v === null) continue;
        let text: string;
        if (Array.isArray(v)) text = v.join(',');
        else if (typeof v === 'number') text = String(v);
        else text = String(v ?? '').trim();
        if (text !== undefined && text !== null) out.push({ questionId: q.id, responseText: text || null });
      }
    }
    return out;
  }

  private persistProgress(): void {
    if (this.submitted || typeof window === 'undefined') return;
    const payload = {
      pageIndex: this.currentPageIndex,
      answers: this.answers
    };
    try {
      window.localStorage.setItem(this.progressKey, JSON.stringify(payload));
    } catch {
      // Ignore storage errors
    }
  }
}
