import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of, map } from 'rxjs';
import { environment } from '../../environments/environment';
import type { AuthResponse, LoginRequest, RegisterRequest } from './api/auth-api.model';

const STORAGE_KEY = 'admin_portal_auth';

export interface AuthUser {
  email: string;
  name: string;
  role?: string;
}

interface StoredAuth {
  token: string;
  user: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = environment.apiUrl;

  constructor(
    private router: Router,
    private http: HttpClient
  ) {}

  register(dto: RegisterRequest): Observable<{ success: true } | { success: false; error: string }> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/api/auth/register`, dto)
      .pipe(
        tap((res) => this.saveSession(res)),
        map(() => ({ success: true as const })),
        catchError((err) => {
          const message =
            err?.error?.message ??
            (typeof err?.error === 'string' ? err.error : null) ??
            err?.status === 409
              ? 'A user with this email already exists.'
              : err?.status === 400
                ? (Array.isArray(err?.error) ? err.error.join(', ') : err?.error) || 'Invalid input.'
                : 'Registration failed. Please try again.';
          return of({ success: false as const, error: message });
        })
      );
  }

  login(email: string, password: string): Observable<{ success: true } | { success: false; error: string }> {
    const body: LoginRequest = { email: (email || '').trim(), password };
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/api/auth/login`, body)
      .pipe(
        tap((res) => this.saveSession(res)),
        map(() => ({ success: true as const })),
        catchError((err) => {
          const message =
            err?.error?.message ??
            (typeof err?.error === 'string' ? err.error : null) ??
            err?.status === 401
              ? 'Invalid email or password.'
              : 'Login failed. Please try again.';
          return of({ success: false as const, error: message });
        })
      );
  }

  private saveSession(res: AuthResponse): void {
    // Support both camelCase (typical) and PascalCase (some .NET configs)
    const raw = res as unknown as Record<string, unknown>;
    const token = (res.token ?? raw['Token']) as string ?? '';
    const u = (res.user ?? raw['User']) as AuthResponse['user'] | undefined;
    if (!u) return;
    const uu = u as unknown as Record<string, unknown>;
    const user: AuthUser = {
      email: (u.email ?? uu['Email']) as string ?? '',
      name: (u.fullName ?? uu['FullName'] ?? u.email ?? uu['Email']) as string ?? '',
      role: (u.role ?? uu['Role']) as string | undefined
    };
    const stored: StoredAuth = { token, user };
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
  }

  logout(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.router.navigate(['/login']);
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  getToken(): string | null {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      const data = JSON.parse(raw) as StoredAuth;
      return data?.token ?? null;
    } catch {
      return null;
    }
  }

  getCurrentUser(): AuthUser | null {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      const data = JSON.parse(raw) as StoredAuth;
      return data?.user ?? null;
    } catch {
      return null;
    }
  }
}
