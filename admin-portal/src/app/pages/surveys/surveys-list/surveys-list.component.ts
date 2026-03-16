import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CardComponent } from '../../../theme/shared/components/card/card.component';
import { SurveyApiService } from '../../../core/services/survey-api.service';
import { UsersApiService } from '../../../core/services/users-api.service';
import type { SurveyDto, CreateSurveyDto, UpdateSurveyDto } from '../../../core/api/survey-api.model';

@Component({
  selector: 'app-surveys-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, CardComponent],
  templateUrl: './surveys-list.component.html',
  styleUrls: ['./surveys-list.component.scss']
})
export class SurveysListComponent implements OnInit {
  surveys: SurveyDto[] = [];
  /** Map of user id -> display name (fullName or email) for "Created by" column */
  createdByNames: Map<number, string> = new Map();
  loading = true;
  error = '';
  createModal = false;
  editSurvey: SurveyDto | null = null;
  newTitle = '';
  newDescription = '';
  /** Closing/end time for new survey (datetime-local format: yyyy-MM-ddTHH:mm). */
  newEndsAt = '';
  /** 0 = Draft, 1 = Active, -1 = Closed. */
  newStatus = 1;
  createSaving = false;
  editSaving = false;
  /** Closing time when editing (datetime-local format). */
  editEndsAt = '';
  editStatus = 1;
  deleteId: number | null = null;

  readonly statusOptions = [
    { value: 0, label: 'Draft' },
    { value: 1, label: 'Active' },
    { value: -1, label: 'Closed' }
  ] as const;

  constructor(
    private api: SurveyApiService,
    private usersApi: UsersApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadSurveys();
    this.usersApi.getUsers().subscribe((users) => {
      const map = new Map<number, string>();
      users.forEach((u) => map.set(u.id, u.fullName?.trim() || u.email || `User #${u.id}`));
      this.createdByNames = map;
      this.cdr.detectChanges();
    });
  }

  loadSurveys(): void {
    this.loading = true;
    this.error = '';
    this.api.getSurveys().subscribe({
      next: (list) => {
        this.surveys = list ?? [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        const status = err?.status ?? err?.statusCode;
        this.error = status === 401 ? 'Please log in to view surveys.' : 'Failed to load surveys. Check that the API is running and you are logged in.';
        this.cdr.detectChanges();
      }
    });
  }

  getCreatedByLabel(s: SurveyDto): string {
    const id = s.researcherId;
    return this.createdByNames.get(id) ?? `User #${id}`;
  }

  openCreate(): void {
    this.newTitle = '';
    this.newDescription = '';
    this.newEndsAt = '';
    this.newStatus = 1;
    this.createModal = true;
  }

  closeCreate(): void {
    this.createModal = false;
  }

  openEdit(s: SurveyDto): void {
    this.editSurvey = s;
    this.newTitle = s.title;
    this.newDescription = s.description ?? '';
    this.editEndsAt = this.toDatetimeLocal(s.endsAt);
    this.editStatus = s.status ?? 1;
  }

  getStatusLabel(status: number | undefined): string {
    if (status === 0) return 'Draft';
    if (status === -1) return 'Closed';
    return 'Active';
  }

  /** Convert ISO endsAt to datetime-local input value (yyyy-MM-ddTHH:mm). */
  toDatetimeLocal(iso: string | null | undefined): string {
    if (!iso) return '';
    try {
      const d = new Date(iso);
      if (Number.isNaN(d.getTime())) return '';
      const y = d.getFullYear();
      const m = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      const h = String(d.getHours()).padStart(2, '0');
      const min = String(d.getMinutes()).padStart(2, '0');
      return `${y}-${m}-${day}T${h}:${min}`;
    } catch {
      return '';
    }
  }

  closeEdit(): void {
    this.editSurvey = null;
  }

  saveEdit(): void {
    if (!this.editSurvey) return;
    const title = this.newTitle?.trim();
    if (!title) return;
    this.editSaving = true;
    const endsAt = this.editEndsAt?.trim() ? new Date(this.editEndsAt).toISOString() : undefined;
    const dto: UpdateSurveyDto = {
      title,
      description: this.newDescription?.trim() || undefined,
      endsAt: endsAt ?? null,
      status: this.editStatus
    };
    this.api.updateSurvey(this.editSurvey.id, dto).subscribe({
      next: (updated) => {
        this.editSaving = false;
        this.closeEdit();
        if (updated) this.loadSurveys();
      },
      error: () => {
        this.editSaving = false;
      }
    });
  }

  createSurvey(): void {
    const title = this.newTitle?.trim();
    if (!title) return;
    this.createSaving = true;
    const endsAt = this.newEndsAt?.trim() ? new Date(this.newEndsAt).toISOString() : undefined;
    const dto: CreateSurveyDto = {
      title,
      description: this.newDescription?.trim() || undefined,
      endsAt: endsAt ?? undefined,
      status: this.newStatus
    };
    this.api.createSurvey(dto).subscribe({
      next: (survey) => {
        this.createSaving = false;
        this.createModal = false;
        if (survey) this.loadSurveys();
      },
      error: () => {
        this.createSaving = false;
        this.createModal = false;
      }
    });
  }

  confirmDelete(id: number): void {
    this.deleteId = id;
  }

  cancelDelete(): void {
    this.deleteId = null;
  }

  deleteSurvey(id: number): void {
    this.api.deleteSurvey(id).subscribe({
      next: (ok) => {
        if (ok) {
          this.deleteId = null;
          this.loadSurveys();
        }
      }
    });
  }

  formatDate(s: string): string {
    if (!s) return '—';
    try {
      return new Date(s).toLocaleDateString();
    } catch {
      return s;
    }
  }
}
