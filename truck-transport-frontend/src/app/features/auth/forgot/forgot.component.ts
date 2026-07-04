import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

type ForgotStep = 'email' | 'reset';

@Component({
  selector: 'app-forgot',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgot.component.html',
  styleUrls: ['./forgot.component.scss'],
})
export class ForgotComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly step = signal<ForgotStep>('email');
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal('');
  readonly backgroundUrl = environment.assets.loginBackground;
  readonly userEmail = signal('');

  readonly emailForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  readonly resetForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required],
  });

  submitEmail(): void {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    this.errorMessage.set('');

    const email = this.emailForm.getRawValue().email;
    this.userEmail.set(email);

    this.auth.forgotPassword({ email }).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        // Prefill the reset form email so submitReset can send it
        this.resetForm.patchValue({ email });
        this.step.set('reset');
      },
      error: () => {
        this.isSubmitting.set(false);
        this.errorMessage.set('Unable to send password reset instructions. Please try again.');
      },
    });
  }

  submitReset(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    const { email, password, confirmPassword } = this.resetForm.getRawValue();
    if (password !== confirmPassword) {
      this.errorMessage.set('Passwords do not match.');
      return;
    }
    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.auth.resetPassword({ email, password, confirmPassword }).subscribe({
      next: (result: any) => {
        this.isSubmitting.set(false);
        if (result.message?.toLowerCase().includes('success')) {
          this.router.navigate(['/login']);
        } else {
          this.errorMessage.set(result.message ?? 'Unable to reset password.');
        }
      },
      error: () => {
        this.isSubmitting.set(false);
        this.errorMessage.set('Unable to reset password. Please try again.');
      },
    });
  }

  backToEmail(): void {
    this.step.set('email');
    this.errorMessage.set('');
  }

  backToLogin(): void {
    this.router.navigate(['/login']);
  }
}
