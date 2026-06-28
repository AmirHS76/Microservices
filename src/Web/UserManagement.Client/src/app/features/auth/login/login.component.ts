import {
  Component,
  inject,
  signal,
  ChangeDetectionStrategy,
} from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { NgIcon } from "@ng-icons/core";
import { Title } from "@angular/platform-browser";
import { finalize } from "rxjs";
import { AuthService } from "../../../core/auth/auth.service";

@Component({
  selector: "app-login",
  imports: [ReactiveFormsModule, RouterLink, NgIcon],
  template: `
    <main class="auth-page">
      <section class="auth-panel">
        <div class="auth-copy">
          <span class="brand-mark">UM</span>
          <h1>Welcome back</h1>
          <p>
            Sign in to manage user profiles, role assignments, and login
            activity through the gateway.
          </p>
        </div>

        <form class="auth-form" [formGroup]="form" (ngSubmit)="submit()">
          <label>
            Email or Username
            <input
              type="text"
              formControlName="usernameOrEmail"
              autocomplete="username"
              placeholder="admin@example.com or admin"
            />
          </label>

          <label>
            Password
            <input
              type="password"
              formControlName="password"
              autocomplete="current-password"
              placeholder="Password"
            />
          </label>

          @if (error()) {
            <p class="form-error">{{ error() }}</p>
          }

          <button
            class="button primary full"
            type="submit"
            [disabled]="form.invalid || loading()"
          >
            <ng-icon name="lucideLogIn" size="18" />
            {{ loading() ? "Signing in..." : "Sign in" }}
          </button>

          <p class="auth-switch">
            Need an account? <a routerLink="/register">Create one</a>
          </p>
        </form>
      </section>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: "../auth-pages.scss",
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly title = inject(Title);
  readonly loading = signal(false);

  constructor() {
    this.title.setTitle('Sign in - User Management');
  }
  readonly error = signal<string | null>(null);
  readonly form = this.fb.nonNullable.group({
    usernameOrEmail: ["", [Validators.required]],
    password: ["", [Validators.required, Validators.minLength(6)]],
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const { usernameOrEmail, password } = this.form.getRawValue();

    const isEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(usernameOrEmail);

    this.auth
      .login(
        isEmail ? usernameOrEmail : null,
        isEmail ? null : usernameOrEmail,
        password,
      )
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            void this.router.navigateByUrl("/users");
            return;
          }

          this.error.set(response.errors.join(", ") || "Unable to sign in.");
        },
        error: (response) =>
          this.error.set(
            response.error?.errors?.join(", ") ||
              response.error?.message ||
              "Unable to sign in.",
          ),
      });
  }
}
