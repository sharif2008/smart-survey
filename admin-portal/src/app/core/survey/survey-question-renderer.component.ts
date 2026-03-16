import { Component, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import type { QuestionDto } from '../api/survey-api.model';
import { QuestionType } from '../api/survey-api.model';

/**
 * Schema-driven question renderer: chooses control by question type and binds answer.
 * Used for preview and participant survey form.
 */
@Component({
  selector: 'app-survey-question-renderer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="mb-3">
      <label class="form-label">
        {{ question().text }}
        @if (question().isRequired) {
          <span class="text-danger">*</span>
        }
      </label>
      @switch (question().type) {
        @case (QuestionType.Text) {
          <input type="text" class="form-control" [ngModel]="value()" (ngModelChange)="valueChange.emit($event)" [placeholder]="question().text" />
        }
        @case (QuestionType.TextArea) {
          <textarea class="form-control" [ngModel]="value()" (ngModelChange)="valueChange.emit($event)" rows="3"></textarea>
        }
        @case (QuestionType.Number) {
          <input type="number" class="form-control" [ngModel]="value()" (ngModelChange)="valueChange.emit($event)" />
        }
        @case (QuestionType.YesNo) {
          <div class="d-flex flex-column gap-1">
            <div class="form-check">
              <input
                type="radio"
                class="form-check-input"
                [name]="'q' + question().id"
                [id]="'q' + question().id + '-yes'"
                value="Yes"
                [ngModel]="value()"
                (ngModelChange)="valueChange.emit($event)"
              />
              <label class="form-check-label" [for]="'q' + question().id + '-yes'">Yes</label>
            </div>
            <div class="form-check">
              <input
                type="radio"
                class="form-check-input"
                [name]="'q' + question().id"
                [id]="'q' + question().id + '-no'"
                value="No"
                [ngModel]="value()"
                (ngModelChange)="valueChange.emit($event)"
              />
              <label class="form-check-label" [for]="'q' + question().id + '-no'">No</label>
            </div>
          </div>
        }
        @case (QuestionType.SingleChoice) {
          <div class="d-flex flex-column gap-1">
            @for (opt of getOptions(); track opt) {
              <div class="form-check">
                <input type="radio" class="form-check-input" [name]="'q' + question().id" [id]="'q' + question().id + '-' + opt" [value]="opt" [ngModel]="value()" (ngModelChange)="valueChange.emit($event)" />
                <label class="form-check-label" [for]="'q' + question().id + '-' + opt">{{ opt }}</label>
              </div>
            }
          </div>
        }
        @case (QuestionType.MultipleChoice) {
          <div class="d-flex flex-column gap-1">
            @for (opt of getOptions(); track opt) {
              <div class="form-check">
                <input type="checkbox" class="form-check-input" [id]="'q' + question().id + '-' + opt" [ngModel]="selectedMultiple().includes(opt)" (ngModelChange)="toggleMultiOption(opt, $event)" />
                <label class="form-check-label" [for]="'q' + question().id + '-' + opt">{{ opt }}</label>
              </div>
            }
          </div>
        }
        @case (QuestionType.Rating) {
          <select class="form-select" [ngModel]="value()" (ngModelChange)="valueChange.emit($event)">
            <option value="">—</option>
            @for (r of [1,2,3,4,5]; track r) {
              <option [value]="r">{{ r }}</option>
            }
          </select>
        }
        @case (QuestionType.Date) {
          <input type="date" class="form-control" [ngModel]="value()" (ngModelChange)="valueChange.emit($event)" />
        }
        @case (QuestionType.Like) {
          <div class="d-flex gap-2">
            <button type="button" class="btn btn-outline-primary" [class.btn-primary]="value() === 'Like'" (click)="valueChange.emit('Like')">Like</button>
            <button type="button" class="btn" [class.btn-outline-secondary]="value() !== 'Dislike'" [class.btn-primary]="value() === 'Dislike'" (click)="valueChange.emit('Dislike')">Dislike</button>
          </div>
        }
        @case (QuestionType.Ranking) {
          <div class="ranking-options">
            @for (opt of getOptions(); track opt) {
              <div class="mb-2 d-flex align-items-center gap-2">
                <label class="small text-muted mb-0" style="min-width: 4rem;">Rank {{ $index + 1 }}</label>
                <select class="form-select form-select-sm" [value]="getRankingValue($index)" (change)="setRankingValue($index, $any($event.target).value)">
                  <option value="">—</option>
                  @for (o of getOptions(); track o) {
                    <option [value]="o">{{ o }}</option>
                  }
                </select>
              </div>
            }
          </div>
        }
        @case (QuestionType.NetPromoterScore) {
          <div class="nps-scale d-flex flex-wrap gap-1">
            @for (n of npsScale; track n) {
              <button type="button" class="btn btn-sm" [class.btn-outline-primary]="value() !== n" [class.btn-primary]="value() === n" (click)="valueChange.emit(n)">{{ n }}</button>
            }
          </div>
          <small class="text-muted">0 = Not at all likely, 10 = Extremely likely</small>
        }
        @default {
          <input type="text" class="form-control" [ngModel]="value()" (ngModelChange)="valueChange.emit($event)" />
        }
      }
    </div>
  `
})
export class SurveyQuestionRendererComponent {
  question = input.required<QuestionDto>();
  value = input<string | number | string[] | null | undefined>('');
  valueChange = output<string | number | string[]>();

  readonly QuestionType = QuestionType;

  constructor() {
    effect(() => {
      const v = this.value();
      if (Array.isArray(v)) this.selectedMultiple.set(v);
    });
  }

  getOptions(): string[] {
    const q = this.question();
    if (!q?.optionsJson) return [];
    try {
      const arr = JSON.parse(q.optionsJson) as string[];
      return Array.isArray(arr) ? arr : [];
    } catch {
      return [];
    }
  }

  selectedMultiple = signal<string[]>([]);

  /** 0–10 scale for Net Promoter Score */
  readonly npsScale = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

  toggleMultiOption(opt: string, checked: boolean): void {
    const current = this.selectedMultiple();
    const next = checked ? [...current, opt] : current.filter((o) => o !== opt);
    this.selectedMultiple.set(next);
    this.valueChange.emit(next);
  }

  /** Ranking: get option at 0-based rank position from stored comma-separated value */
  getRankingValue(rankIndex: number): string {
    const v = this.value();
    const str = typeof v === 'string' ? v : Array.isArray(v) ? (v as string[]).join(',') : '';
    const parts = str ? str.split(',').map((s) => s.trim()).filter(Boolean) : [];
    return parts[rankIndex] ?? '';
  }

  /** Ranking: set option at rank position and emit full comma-separated ranking */
  setRankingValue(rankIndex: number, option: string): void {
    const opts = this.getOptions();
    const v = this.value();
    const parts = (typeof v === 'string' ? v.split(',').map((s) => s.trim()) : Array.isArray(v) ? [...v] : []).filter(Boolean);
    while (parts.length < opts.length) parts.push('');
    parts[rankIndex] = option || '';
    const next = parts.filter(Boolean);
    this.valueChange.emit(next.join(','));
  }
}
