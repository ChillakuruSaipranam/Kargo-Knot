import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

type AuthMode = 'login' | 'register';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly logoUrl = environment.assets.logo;
  readonly backgroundUrl = environment.assets.loginBackground;

  readonly mode = signal<AuthMode>('login');
  readonly errorMessage = signal('');
  readonly isSubmitting = signal(false);

  readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  readonly registerForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  setMode(next: AuthMode): void {
    this.mode.set(next);
    this.errorMessage.set('');
  }

  submitLogin(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.auth.login(this.loginForm.getRawValue()).subscribe({
      next: (result) => {
        this.isSubmitting.set(false);
        if (!result.success) {
          this.errorMessage.set(result.message ?? 'Invalid credentials.');
          return;
        }
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.isSubmitting.set(false);
        this.errorMessage.set('Unable to reach the server. Please try again.');
      },
    });
  }

  submitRegister(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.auth.register(this.registerForm.getRawValue()).subscribe({
      next: (result) => {
        this.isSubmitting.set(false);
        if (!result.success) {
          this.errorMessage.set(result.message ?? 'Registration failed.');
          return;
        }
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.isSubmitting.set(false);
        this.errorMessage.set('Unable to reach the server. Please try again.');
      },
    });
  }
}
