import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LogIn, LucideAngularModule } from 'lucide-angular';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule],
  template: `
    <main class="auth-page">
      <section class="auth-panel">
        <div class="auth-copy">
          <span class="brand-mark">UM</span>
          <h1>Welcome back</h1>
          <p>Sign in to manage user profiles, role assignments, and login activity through the gateway.</p>
        </div>

        <form class="auth-form" [formGroup]="form" (ngSubmit)="submit()">
          <label>
            Email
            <input type="email" formControlName="email" autocomplete="email" placeholder="admin@example.com" />
          </label>

          <label>
            Password
            <input type="password" formControlName="password" autocomplete="current-password" placeholder="Password" />
          </label>

          @if (error()) {
            <p class="form-error">{{ error() }}</p>
          }

          <button class="button primary full" type="submit" [disabled]="form.invalid || loading()">
            <lucide-icon [img]="loginIcon" size="18" />
            {{ loading() ? 'Signing in...' : 'Sign in' }}
          </button>

          <p class="auth-switch">Need an account? <a routerLink="/register">Create one</a></p>
        </form>
      </section>
    </main>
  `,
  styleUrl: '../auth-pages.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly loginIcon = LogIn;
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();

    this.auth
      .login(email, password)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            void this.router.navigateByUrl('/users');
            return;
          }

          this.error.set(response.errors.join(', ') || 'Unable to sign in.');
        },
        error: (response) => this.error.set(response.error?.errors?.join(', ') || response.error?.message || 'Unable to sign in.')
      });
  }
}
