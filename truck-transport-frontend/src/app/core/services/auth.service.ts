import { Injectable, signal, computed } from '@angular/core';
import { AuthUser, LoginRequest } from '../models/user.model';

const AUTH_KEY = 'transport_auth';

interface StoredUser extends AuthUser {}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUser = signal<StoredUser | null>(this.loadUser());

  readonly user = this.currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUser() !== null);
  readonly isAdmin = computed(() => this.currentUser()?.role === 'Admin');

  login(request: LoginRequest): boolean {
    const account = this.mockAccounts.find(
      (entry) => entry.email === request.email && entry.password === request.password
    );

    if (!account) {
      return false;
    }

    const user: StoredUser = {
      id: account.id,
      email: account.email,
      fullName: account.fullName,
      role: account.role,
      token: `mock-jwt-${account.role.toLowerCase()}-${Date.now()}`,
    };

    this.currentUser.set(user);
    localStorage.setItem(AUTH_KEY, JSON.stringify(user));
    return true;
  }

  logout(): void {
    this.currentUser.set(null);
    localStorage.removeItem(AUTH_KEY);
  }

  getToken(): string | null {
    return this.currentUser()?.token ?? null;
  }

  private loadUser(): StoredUser | null {
    const raw = localStorage.getItem(AUTH_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as StoredUser;
    } catch {
      return null;
    }
  }

  private readonly mockAccounts = [
    {
      id: '1',
      email: 'admin@transport.com',
      password: 'admin123',
      fullName: 'System Administrator',
      role: 'Admin' as const,
    },
    {
      id: '2',
      email: 'user@transport.com',
      password: 'user123',
      fullName: 'Operations User',
      role: 'User' as const,
    },
  ];
}
