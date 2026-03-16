import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, map, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { UserResponse, RegisterRequest } from '../api/auth-api.model';

/** Normalize user from API (handles PascalCase or camelCase). */
function toUserResponse(raw: Record<string, unknown>): UserResponse {
  const createdAt = raw['createdAt'] ?? raw['CreatedAt'];
  let createdAtStr = '';
  if (typeof createdAt === 'string') createdAtStr = createdAt;
  else if (createdAt instanceof Date) createdAtStr = createdAt.toISOString();
  else if (createdAt != null) createdAtStr = String(createdAt);
  return {
    id: Number((raw['id'] ?? raw['Id']) ?? 0),
    fullName: String(raw['fullName'] ?? raw['FullName'] ?? ''),
    email: String(raw['email'] ?? raw['Email'] ?? ''),
    role: String(raw['role'] ?? raw['Role'] ?? ''),
    createdAt: createdAtStr
  };
}

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getUsers(): Observable<UserResponse[]> {
    return this.http.get<unknown[]>(`${this.apiUrl}/api/users`).pipe(
      map((list) => (Array.isArray(list) ? list.map((u) => toUserResponse(u as Record<string, unknown>)) : [])),
      catchError((err) => throwError(() => err))
    );
  }

  getUser(id: number): Observable<UserResponse | null> {
    return this.http.get<UserResponse>(`${this.apiUrl}/api/users/${id}`).pipe(
      catchError(() => of(null))
    );
  }

  createUser(dto: RegisterRequest): Observable<UserResponse | null> {
    return this.http
      .post<UserResponse>(`${this.apiUrl}/api/users`, dto)
      .pipe(catchError(() => of(null)));
  }

  updateUser(id: number, dto: RegisterRequest): Observable<UserResponse | null> {
    return this.http
      .put<UserResponse>(`${this.apiUrl}/api/users/${id}`, dto)
      .pipe(catchError(() => of(null)));
  }

  deleteUser(id: number): Observable<boolean> {
    return this.http
      .delete(`${this.apiUrl}/api/users/${id}`, { observe: 'response' })
      .pipe(
        map((r) => r.status === 204),
        catchError(() => of(false))
      );
  }
}
