import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CardComponent } from '../../../theme/shared/components/card/card.component';
import { SurveyApiService } from '../../../core/services/survey-api.service';
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
  loading = true;
  error = '';
  createModal = false;
  editSurvey: SurveyDto | null = null;
  newTitle = '';
  newDescription = '';
  createSaving = false;
  editSaving = false;
  deleteId: number | null = null;

  constructor(private api: SurveyApiService) {}

  ngOnInit(): void {
    this.loadSurveys();
  }

  loadSurveys(): void {
    this.loading = true;
    this.error = '';
    this.api.getSurveys().subscribe({
      next: (list) => {
        this.surveys = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load surveys.';
        this.loading = false;
      }
    });
  }

  openCreate(): void {
    this.newTitle = '';
    this.newDescription = '';
    this.createModal = true;
  }

  closeCreate(): void {
    this.createModal = false;
  }

  openEdit(s: SurveyDto): void {
    this.editSurvey = s;
    this.newTitle = s.title;
    this.newDescription = s.description ?? '';
  }

  closeEdit(): void {
    this.editSurvey = null;
  }

  saveEdit(): void {
    if (!this.editSurvey) return;
    const title = this.newTitle?.trim();
    if (!title) return;
    this.editSaving = true;
    const dto: UpdateSurveyDto = { title, description: this.newDescription?.trim() || undefined };
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
    const dto: CreateSurveyDto = { title, description: this.newDescription?.trim() || undefined };
    this.api.createSurvey(dto).subscribe({
      next: (survey) => {
        this.createSaving = false;
        this.createModal = false;
        if (survey) this.loadSurveys();
      },
      error: () => {
        this.createSaving = false;
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
