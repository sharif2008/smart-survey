import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { UserResponse, RegisterRequest } from '../api/auth-api.model';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getUsers(): Observable<UserResponse[]> {
    return this.http.get<UserResponse[]>(`${this.apiUrl}/api/users`).pipe(
      catchError(() => of([]))
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
