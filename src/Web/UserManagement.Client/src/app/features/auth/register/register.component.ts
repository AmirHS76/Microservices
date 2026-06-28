import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { Title } from '@angular/platform-browser';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
    selector: 'app-register',
    imports: [ReactiveFormsModule, RouterLink, NgIcon],
    template: `
    <main class="auth-page">
      <section class="auth-panel">
        <div class="auth-copy">
          <span class="brand-mark">UM</span>
          <h1>Create account</h1>
          <p>Register a new identity user and start managing the synchronized profile projection.</p>
        </div>

        <form class="auth-form" [formGroup]="form" (ngSubmit)="submit()">
          <label>
            Username
            <input type="text" formControlName="username" autocomplete="username" placeholder="Display name" />
          </label>

          <label>
            Email
            <input type="email" formControlName="email" autocomplete="email" placeholder="name@example.com" />
          </label>

          <label>
            Password
            <input type="password" formControlName="password" autocomplete="new-password" placeholder="At least 6 characters" />
          </label>

          <label>
            Confirm password
            <input type="password" formControlName="confirmPassword" autocomplete="new-password" placeholder="Repeat password" />
          </label>

          @if (passwordMismatch()) {
            <p class="form-error">Passwords must match.</p>
          }

          @if (error()) {
            <p class="form-error">{{ error() }}</p>
          }

          <button class="button primary full" type="submit" [disabled]="form.invalid || passwordMismatch() || loading()">
            <ng-icon name="lucideUserPlus" size="18" />
            {{ loading() ? 'Creating...' : 'Create account' }}
          </button>

          <p class="auth-switch">Already registered? <a routerLink="/login">Sign in</a></p>
        </form>
      </section>
    </main>
  `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: '../auth-pages.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly title = inject(Title);
  readonly loading = signal(false);

  constructor() {
    this.title.setTitle('Create account - User Management');
  }
  readonly error = signal<string | null>(null);
  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  });
  passwordMismatch(): boolean {
    const { password, confirmPassword } = this.form.getRawValue();
    return !!confirmPassword && password !== confirmPassword;
  }

  submit(): void {
    if (this.form.invalid || this.passwordMismatch()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    const { username, email, password } = this.form.getRawValue();

    this.auth
      .register(username, email, password)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            void this.router.navigateByUrl('/login');
            return;
          }

          this.error.set(response.errors.join(', ') || 'Unable to create account.');
        },
        error: (response) =>
          this.error.set(response.error?.errors?.join(', ') || response.error?.message || 'Unable to create account.')
      });
  }
}
