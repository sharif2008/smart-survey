import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardComponent } from '../../../theme/shared/components/card/card.component';
import { UsersApiService } from '../../../core/services/users-api.service';
import { AuthService } from '../../../core/auth.service';
import type { UserResponse, RegisterRequest } from '../../../core/api/auth-api.model';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [CommonModule, FormsModule, CardComponent],
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss']
})
export class UsersListComponent implements OnInit {
  users: UserResponse[] = [];
  loading = true;
  error = '';
  isAdmin = false;
  createModal = false;
  editUser: UserResponse | null = null;
  fullName = '';
  email = '';
  password = '';
  role: 'Admin' | 'Researcher' = 'Researcher';
  createSaving = false;
  editSaving = false;
  deleteId: number | null = null;
  roles = [
    { value: 'Researcher' as const, label: 'Researcher' },
    { value: 'Admin' as const, label: 'Admin' }
  ];

  constructor(
    private api: UsersApiService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const user = this.auth.getCurrentUser();
    this.isAdmin = user?.role === 'Admin';
    if (!this.isAdmin) {
      this.error = 'Only Admins can view the users list.';
      this.loading = false;
      return;
    }
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = '';
    this.api.getUsers().subscribe({
      next: (list) => {
        this.users = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load users.';
        this.loading = false;
      }
    });
  }

  openCreate(): void {
    this.fullName = '';
    this.email = '';
    this.password = '';
    this.role = 'Researcher';
    this.createModal = true;
  }

  closeCreate(): void {
    this.createModal = false;
  }

  createUser(): void {
    const fullName = this.fullName?.trim();
    const email = this.email?.trim();
    const password = this.password;
    if (!fullName || !email || !password || password.length < 6) return;
    this.createSaving = true;
    const dto: RegisterRequest = { fullName, email, password, role: this.role };
    this.api.createUser(dto).subscribe({
      next: (u) => {
        this.createSaving = false;
        this.closeCreate();
        if (u) this.loadUsers();
      },
      error: () => {
        this.createSaving = false;
      }
    });
  }

  openEdit(u: UserResponse): void {
    this.editUser = u;
    this.fullName = u.fullName;
    this.email = u.email;
    this.password = '';
    this.role = (u.role === 'Admin' ? 'Admin' : 'Researcher') as 'Admin' | 'Researcher';
  }

  closeEdit(): void {
    this.editUser = null;
  }

  saveEdit(): void {
    if (!this.editUser) return;
    const fullName = this.fullName?.trim();
    const email = this.email?.trim();
    const password = this.password;
    if (!fullName || !email || !password || password.length < 6) return;
    this.editSaving = true;
    const dto: RegisterRequest = { fullName, email, password, role: this.role };
    this.api.updateUser(this.editUser.id, dto).subscribe({
      next: (updated) => {
        this.editSaving = false;
        this.closeEdit();
        if (updated) this.loadUsers();
      },
      error: () => {
        this.editSaving = false;
      }
    });
  }

  confirmDelete(id: number): void {
    this.deleteId = id;
  }

  cancelDelete(): void {
    this.deleteId = null;
  }

  deleteUser(id: number): void {
    this.api.deleteUser(id).subscribe({
      next: (ok) => {
        if (ok) {
          this.deleteId = null;
          this.loadUsers();
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
