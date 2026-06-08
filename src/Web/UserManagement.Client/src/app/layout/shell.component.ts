import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LogOut, LucideAngularModule, MessageCircle, ShieldCheck, Users } from 'lucide-angular';
import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideAngularModule],
  template: `
    <main class="app-shell">
      <aside class="sidebar">
        <a class="brand" routerLink="/users" aria-label="User Management home">
          <span class="brand-mark">UM</span>
          <span>
            <strong>User Console</strong>
            <small>Microservice gateway</small>
          </span>
        </a>

        <nav class="nav-list" aria-label="Primary navigation">
          <a routerLink="/users" routerLinkActive="active">
            <lucide-icon [img]="usersIcon" size="18" />
            Users
          </a>
          <a routerLink="/chat" routerLinkActive="active">
            <lucide-icon [img]="chatIcon" size="18" />
            Chat
          </a>
        </nav>

        <section class="session-card">
          <div class="avatar">{{ initials() }}</div>
          <div class="session-copy">
            <strong>{{ email() }}</strong>
            <span>{{ roles() }}</span>
          </div>
        </section>
      </aside>

      <section class="workspace">
        <header class="topbar">
          <div>
            <span class="eyebrow"><lucide-icon [img]="shieldIcon" size="14" /> Secure Admin Area</span>
            <h1>User management</h1>
          </div>

          <button class="button ghost" type="button" (click)="auth.logout()">
            <lucide-icon [img]="logoutIcon" size="18" />
            Sign out
          </button>
        </header>

        <router-outlet />
      </section>
    </main>
  `,
  styles: [
    `
      .app-shell {
        min-height: 100vh;
        display: grid;
        grid-template-columns: 280px minmax(0, 1fr);
        background:
          radial-gradient(circle at 12% 12%, rgba(56, 189, 248, 0.14), transparent 34rem),
          var(--color-bg);
      }

      .sidebar {
        position: sticky;
        top: 0;
        height: 100vh;
        padding: 24px;
        border-right: 1px solid var(--color-border);
        background: rgba(10, 15, 27, 0.82);
        backdrop-filter: blur(18px);
        display: flex;
        flex-direction: column;
        gap: 28px;
      }

      .brand {
        display: flex;
        align-items: center;
        gap: 12px;
        color: var(--color-text);
        text-decoration: none;
      }

      .brand-mark {
        width: 44px;
        height: 44px;
        border-radius: 8px;
        display: grid;
        place-items: center;
        background: linear-gradient(135deg, var(--color-accent), var(--color-warm));
        color: #06111f;
        font-weight: 900;
      }

      .brand small,
      .session-copy span {
        display: block;
        color: var(--color-muted);
        margin-top: 2px;
      }

      .nav-list {
        display: grid;
        gap: 8px;
      }

      .nav-list a {
        height: 44px;
        border-radius: 8px;
        padding: 0 12px;
        display: flex;
        align-items: center;
        gap: 10px;
        color: var(--color-muted);
        text-decoration: none;
      }

      .nav-list a.active,
      .nav-list a:hover {
        background: var(--color-panel);
        color: var(--color-text);
      }

      .session-card {
        margin-top: auto;
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 14px;
        border: 1px solid var(--color-border);
        border-radius: 8px;
        background: rgba(15, 23, 42, 0.7);
      }

      .avatar {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        display: grid;
        place-items: center;
        background: rgba(250, 204, 21, 0.12);
        color: var(--color-warm);
        font-weight: 800;
      }

      .workspace {
        min-width: 0;
        padding: 28px;
      }

      .topbar {
        min-height: 72px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 20px;
        margin-bottom: 24px;
      }

      .topbar h1 {
        margin: 6px 0 0;
        font-size: 30px;
      }

      .eyebrow {
        color: var(--color-muted);
        display: inline-flex;
        align-items: center;
        gap: 8px;
        font-size: 13px;
      }

      @media (max-width: 860px) {
        .app-shell {
          grid-template-columns: 1fr;
        }

        .sidebar {
          position: static;
          height: auto;
          padding: 16px;
        }

        .workspace {
          padding: 18px;
        }

        .topbar {
          align-items: flex-start;
          flex-direction: column;
        }
      }
    `
  ]
})
export class ShellComponent {
  readonly auth = inject(AuthService);
  readonly usersIcon = Users;
  readonly chatIcon = MessageCircle;
  readonly shieldIcon = ShieldCheck;
  readonly logoutIcon = LogOut;
  readonly email = computed(() => this.auth.currentUser()?.email || 'Signed in');
  readonly roles = computed(() => this.auth.currentUser()?.roles.join(', ') || 'User');
  readonly initials = computed(() => (this.email().slice(0, 2) || 'UM').toUpperCase());
}
