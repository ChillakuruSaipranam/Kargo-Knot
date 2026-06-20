import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal, computed } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthUser, LoginRequest, RegisterRequest } from '../models/user.model';

const AUTH_KEY = 'transport_auth';

interface StoredUser extends AuthUser {}

interface ApiLoginResponse {
  token: string;
  user: {
    id: string;
    email: string;
    fullName: string;
    role: string;
  };
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUser = signal<StoredUser | null>(this.loadUser());

  readonly user = this.currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUser() !== null);
  readonly isAdmin = computed(() => this.currentUser()?.role === 'Admin');

  login(request: LoginRequest): Observable<{ success: boolean; message?: string }> {
    return this.http.post<ApiLoginResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap((response) => this.persistUser(response)),
      map(() => ({ success: true })),
      catchError((err: HttpErrorResponse) =>
        of({ success: false, message: err.error?.message ?? 'Invalid credentials.' })
      )
    );
  }

  register(request: RegisterRequest): Observable<{ success: boolean; message?: string }> {
    return this.http.post<ApiLoginResponse>(`${environment.apiUrl}/auth/register`, request).pipe(
      tap((response) => this.persistUser(response)),
      map(() => ({ success: true })),
      catchError((err: HttpErrorResponse) =>
        of({ success: false, message: err.error?.message ?? 'Registration failed.' })
      )
    );
  }

  logout(): void {
    this.currentUser.set(null);
    localStorage.removeItem(AUTH_KEY);
  }

  getToken(): string | null {
    return this.currentUser()?.token ?? null;
  }

  private persistUser(response: ApiLoginResponse): void {
    const user: StoredUser = {
      id: response.user.id,
      email: response.user.email,
      fullName: response.user.fullName,
      role: response.user.role as AuthUser['role'],
      token: response.token,
    };
    this.currentUser.set(user);
    localStorage.setItem(AUTH_KEY, JSON.stringify(user));
  }

  private loadUser(): StoredUser | null {
    const raw = localStorage.getItem(AUTH_KEY);
    if (!raw) {
      return null;
    }
    try {
      const user = JSON.parse(raw) as StoredUser;
      return user.token?.startsWith('mock-jwt-') ? null : user;
    } catch {
      return null;
    }
  }
}
