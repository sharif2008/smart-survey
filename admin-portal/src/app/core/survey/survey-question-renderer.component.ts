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
  styles: [`
    .rating-stars { font-size: 1.75rem; line-height: 1; }
    .rating-star-btn { cursor: pointer; color: #dee2e6; transition: color 0.15s ease, transform 0.1s ease; }
    .rating-star-btn:hover { color: #ffc107; transform: scale(1.1); }
    .rating-star-btn.filled { color: #ffc107; }
    .rating-star { user-select: none; }
    .ranking-dnd-list { display: flex; flex-direction: column; gap: 0.5rem; }
    .ranking-dnd-item {
      display: flex; align-items: center; gap: 0.75rem;
      padding: 0.6rem 0.9rem; border-radius: 0.5rem;
      background: #fff; border: 1px solid rgba(148, 163, 184, 0.35);
      cursor: grab; user-select: none; transition: box-shadow 0.15s ease, border-color 0.15s ease;
    }
    .ranking-dnd-item:hover { border-color: rgba(13, 110, 253, 0.4); box-shadow: 0 2px 8px rgba(13, 110, 253, 0.1); }
    .ranking-dnd-item:active { cursor: grabbing; }
    .ranking-dnd-item.dragging { opacity: 0.6; box-shadow: 0 8px 20px rgba(0,0,0,0.12); }
    .ranking-dnd-item.drag-over { border-color: #0d6efd; background: rgba(13, 110, 253, 0.06); }
    .ranking-dnd-handle { color: #94a3b8; font-size: 1.1rem; line-height: 1; }
    .ranking-dnd-rank { font-size: 0.75rem; font-weight: 600; color: #64748b; min-width: 2rem; }
    .ranking-dnd-label { flex: 1; }
  `],
  template: `
    <div class="mb-3">
      <label class="form-label">
        {{ question().text || 'Untitled question' }}
        @if (question().isRequired) {
          <span class="text-danger">*</span>
        }
      </label>
      @switch (question().type) {
        @case (QuestionType.Text) {
          <input
            type="text"
            class="form-control"
            [ngModel]="value()"
            (ngModelChange)="valueChange.emit($event)"
            [placeholder]="question().text || 'Untitled question'"
          />
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
          <div class="rating-stars d-flex gap-1 align-items-center" role="group" aria-label="Rating 1 to 5">
            @for (r of [1,2,3,4,5]; track r) {
              <button type="button" class="rating-star-btn p-0 border-0 bg-transparent" [class.filled]="ratingValue() >= r" [attr.aria-pressed]="ratingValue() === r" [attr.aria-label]="'Rate ' + r + ' out of 5'" (click)="valueChange.emit(r)">
                <span class="rating-star" aria-hidden="true">{{ ratingValue() >= r ? '★' : '☆' }}</span>
              </button>
            }
          </div>
          @if (ratingValue()) {
            <small class="text-muted d-block mt-1">{{ ratingValue() }} of 5</small>
          }
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
          <div class="ranking-dnd-list" role="list">
            @for (opt of getRankingOrder(); track opt; let i = $index) {
              <div
                class="ranking-dnd-item"
                [class.dragging]="rankingDraggedIndex() === i"
                [class.drag-over]="rankingDropIndex() === i && rankingDraggedIndex() !== null"
                draggable="true"
                role="listitem"
                [attr.aria-label]="'Rank ' + (i + 1) + ': ' + opt + '. Drag to reorder.'"
                (dragstart)="rankingDragStart($event, i)"
                (dragend)="rankingDragEnd()"
                (dragover)="rankingDragOver($event, i)"
                (dragleave)="rankingDragLeave(i)"
                (drop)="rankingDrop($event, i)"
              >
                <span class="ranking-dnd-handle" aria-hidden="true">⋮⋮</span>
                <span class="ranking-dnd-rank">{{ i + 1 }}</span>
                <span class="ranking-dnd-label">{{ opt }}</span>
              </div>
            }
          </div>
          <small class="text-muted d-block mt-2">Drag items to set your order (top = first choice).</small>
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

  /** Drag-and-drop ranking: index being dragged, or null */
  rankingDraggedIndex = signal<number | null>(null);
  /** Drag-over target index for visual feedback */
  rankingDropIndex = signal<number | null>(null);

  /** 0–10 scale for Net Promoter Score */
  readonly npsScale = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

  /** Current rating value (1–5) for Rating question type */
  ratingValue(): number {
    const v = this.value();
    if (v == null || v === '') return 0;
    const n = typeof v === 'number' ? v : Number(v);
    return Number.isFinite(n) && n >= 1 && n <= 5 ? n : 0;
  }

  toggleMultiOption(opt: string, checked: boolean): void {
    const current = this.selectedMultiple();
    const next = checked ? [...current, opt] : current.filter((o) => o !== opt);
    this.selectedMultiple.set(next);
    this.valueChange.emit(next);
  }

  /** Ranking: current ordered list (from value or default options order). Used for drag-and-drop display. */
  getRankingOrder(): string[] {
    const opts = this.getOptions();
    if (opts.length === 0) return [];
    const v = this.value();
    const str = typeof v === 'string' ? v : Array.isArray(v) ? (v as string[]).join(',') : '';
    const parts = str ? str.split(',').map((s) => s.trim()).filter(Boolean) : [];
    if (parts.length === 0) return [...opts];
    const set = new Set(parts);
    const rest = opts.filter((o) => !set.has(o));
    return [...parts, ...rest];
  }

  rankingDragStart(ev: DragEvent, index: number): void {
    this.rankingDraggedIndex.set(index);
    if (ev.dataTransfer) {
      ev.dataTransfer.effectAllowed = 'move';
      ev.dataTransfer.setData('text/plain', String(index));
    }
  }

  rankingDragEnd(): void {
    this.rankingDraggedIndex.set(null);
    this.rankingDropIndex.set(null);
  }

  rankingDragOver(ev: DragEvent, index: number): void {
    ev.preventDefault();
    if (ev.dataTransfer) ev.dataTransfer.dropEffect = 'move';
    if (this.rankingDraggedIndex() !== null) this.rankingDropIndex.set(index);
  }

  rankingDragLeave(_index: number): void {
    this.rankingDropIndex.set(null);
  }

  rankingDrop(ev: DragEvent, toIndex: number): void {
    ev.preventDefault();
    const fromIndex = this.rankingDraggedIndex();
    if (fromIndex === null) return;
    const order = this.getRankingOrder();
    if (fromIndex === toIndex || fromIndex < 0 || toIndex < 0 || fromIndex >= order.length || toIndex >= order.length) {
      this.rankingDraggedIndex.set(null);
      this.rankingDropIndex.set(null);
      return;
    }
    const item = order[fromIndex];
    const next = order.filter((_, i) => i !== fromIndex);
    next.splice(toIndex, 0, item);
    this.valueChange.emit(next.join(','));
    this.rankingDraggedIndex.set(null);
    this.rankingDropIndex.set(null);
  }

  /** Ranking: get option at 0-based rank position from stored comma-separated value */
  getRankingValue(rankIndex: number): string {
    const order = this.getRankingOrder();
    return order[rankIndex] ?? '';
  }

  /** Ranking: set option at rank position and emit full comma-separated ranking */
  setRankingValue(rankIndex: number, option: string): void {
    const order = this.getRankingOrder();
    const next = [...order];
    next[rankIndex] = option || '';
    this.valueChange.emit(next.filter(Boolean).join(','));
  }
}
