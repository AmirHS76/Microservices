import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { Title } from '@angular/platform-browser';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { LoginAudit, PaginationMetadata, UserProfile } from '../../core/models/api.models';
import { UserApiService } from '../../core/models/user-api.service';

type SortKey = 'username' | 'email';

@Component({
    selector: 'app-user-management',
    imports: [ReactiveFormsModule, RouterLink, NgIcon, DatePipe],
    template: `
    <section class="toolbar">
      <div class="metric">
        <span>Total users</span>
        <strong>{{ userPagination()?.totalItems ?? users().length }}</strong>
      </div>
      <div class="metric">
        <span>Visible</span>
        <strong>{{ filteredUsers().length }}</strong>
      </div>
      <div class="metric">
        <span>Login audits</span>
        <strong>{{ auditPagination()?.totalItems ?? audits().length }}</strong>
      </div>

      <div class="toolbar-actions">
        <button class="button ghost" type="button" (click)="exportCsv()" [disabled]="!filteredUsers().length">
          <ng-icon name="lucideDownload" size="18" />
          Export
        </button>
        <button class="button primary" type="button" (click)="reload()" [disabled]="loading()">
          <ng-icon name="lucideRefreshCw" size="18" />
          {{ loading() ? 'Loading...' : 'Refresh' }}
        </button>
      </div>
    </section>

    <section class="management-grid">
      <article class="panel users-panel">
        <div class="panel-header">
          <div>
            <span class="eyebrow"><ng-icon name="lucideUsers" size="14" /> Profiles</span>
            <h2>Users</h2>
          </div>

          <label class="search-box">
            <ng-icon name="lucideSearch" size="18" />
            <input type="search" [value]="query()" (input)="query.set($any($event.target).value)" placeholder="Search users" />
          </label>
        </div>

        @if (error()) {
          <p class="notice danger">{{ error() }}</p>
        }

        <div class="table-frame">
          <table>
            <thead>
              <tr>
                <th>
                  <button type="button" class="table-sort" (click)="setSort('username')">Username</button>
                </th>
                <th>
                  <button type="button" class="table-sort" (click)="setSort('email')">Email</button>
                </th>
                <th>User id</th>
              </tr>
            </thead>
            <tbody>
              @for (user of filteredUsers(); track user.userId) {
                <tr [class.selected]="selectedUser()?.userId === user.userId" (click)="selectUser(user)">
                  <td>{{ user.username || 'Pending profile name' }}</td>
                  <td>{{ user.email }}</td>
                  <td><code>{{ user.userId }}</code></td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="3" class="empty-state">No users match the current filter.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="pager">
          <button class="button ghost" type="button" (click)="previousUsersPage()" [disabled]="!userPagination()?.hasPreviousPage">
            Previous
          </button>
          <span>Page {{ userPagination()?.pageNumber ?? 1 }} of {{ userPagination()?.totalPages || 1 }}</span>
          <button class="button ghost" type="button" (click)="nextUsersPage()" [disabled]="!userPagination()?.hasNextPage">
            Next
          </button>
          <label class="page-size">
            Rows
            <select [value]="userPageSize()" (change)="changeUserPageSize($any($event.target).value)">
              <option value="5">5</option>
              <option value="10">10</option>
              <option value="25">25</option>
              <option value="50">50</option>
            </select>
          </label>
        </div>
      </article>

      <aside class="side-stack">
        <article class="panel">
          <div class="panel-header compact">
            <div>
              <span class="eyebrow"><ng-icon name="lucideShieldPlus" size="14" /> Roles</span>
              <h2>Assign role</h2>
            </div>
          </div>

          @if (!auth.isAdmin()) {
            <p class="notice">Role assignment is available after signing in with an Admin token.</p>
          }

          <form class="role-form" [formGroup]="roleForm" (ngSubmit)="assignRole()">
            <label>
              User id
              <input formControlName="userId" placeholder="Select a user or paste an id" />
            </label>

            <label>
              Role
              <select formControlName="role">
                <option value="User">User</option>
                <option value="Admin">Admin</option>
              </select>
            </label>

            @if (roleMessage()) {
              <p class="notice" [class.success]="roleSuccess()">{{ roleMessage() }}</p>
            }

            <button class="button primary full" type="submit" [disabled]="roleForm.invalid || assigningRole() || !auth.isAdmin()">
              <ng-icon name="lucideShieldPlus" size="18" />
              {{ assigningRole() ? 'Assigning...' : 'Assign role' }}
            </button>
          </form>
        </article>

        <article class="panel">
          <div class="panel-header compact">
            <div>
              <span class="eyebrow"><ng-icon name="lucideUserPlus" size="14" /> Create</span>
              <h2>Add user</h2>
            </div>
          </div>
          <p class="notice">New users are created from the register page and synchronized into this profile list by the service event flow.</p>
          <a class="button ghost full link-button" routerLink="/register">
            <ng-icon name="lucideUserPlus" size="18" />
            Register user
          </a>
        </article>
      </aside>
    </section>

    <section class="panel audits-panel">
      <div class="panel-header">
        <div>
          <span class="eyebrow"><ng-icon name="lucideShieldPlus" size="14" /> Admin audit trail</span>
          <h2>Login audits</h2>
        </div>
      </div>

      @if (!auth.isAdmin()) {
        <p class="notice">Audit data requires the Admin role.</p>
      } @else {
        <div class="audit-list">
          @for (audit of audits(); track audit.userId + audit.occurredAtUtc) {
            <div class="audit-row">
              <span>{{ audit.username }}</span>
              <strong>{{ audit.occurredAtUtc | date: 'medium' }}</strong>
            </div>
          } @empty {
            <p class="empty-state">No login audit records returned yet.</p>
          }
        </div>

        <div class="pager">
          <button class="button ghost" type="button" (click)="previousAuditPage()" [disabled]="!auditPagination()?.hasPreviousPage">
            Previous
          </button>
          <span>Page {{ auditPagination()?.pageNumber ?? 1 }} of {{ auditPagination()?.totalPages || 1 }}</span>
          <button class="button ghost" type="button" (click)="nextAuditPage()" [disabled]="!auditPagination()?.hasNextPage">
            Next
          </button>
          <label class="page-size">
            Rows
            <select [value]="auditPageSize()" (change)="changeAuditPageSize($any($event.target).value)">
              <option value="5">5</option>
              <option value="10">10</option>
              <option value="25">25</option>
              <option value="50">50</option>
            </select>
          </label>
        </div>
      }
    </section>
  `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrl: './user-management.component.scss'
})
export class UserManagementComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(UserApiService);
  private readonly title = inject(Title);
  readonly auth = inject(AuthService);
  readonly users = signal<UserProfile[]>([]);
  readonly audits = signal<LoginAudit[]>([]);
  readonly userPagination = signal<PaginationMetadata | null>(null);
  readonly auditPagination = signal<PaginationMetadata | null>(null);
  readonly userPageNumber = signal(1);
  readonly userPageSize = signal(10);
  readonly auditPageNumber = signal(1);
  readonly auditPageSize = signal(10);
  readonly loading = signal(false);
  readonly assigningRole = signal(false);
  readonly error = signal<string | null>(null);
  readonly roleMessage = signal<string | null>(null);
  readonly roleSuccess = signal(false);
  readonly query = signal('');
  readonly sortKey = signal<SortKey>('username');
  readonly selectedUser = signal<UserProfile | null>(null);

  readonly roleForm = this.fb.nonNullable.group({
    userId: ['', Validators.required],
    role: ['User', Validators.required]
  });

  readonly filteredUsers = computed(() => {
    const term = this.query().trim().toLowerCase();
    const key = this.sortKey();

    return this.users()
      .filter((user) => {
        const haystack = `${user.username} ${user.email} ${user.userId}`.toLowerCase();
        return !term || haystack.includes(term);
      })
      .sort((left, right) => String(left[key] ?? '').localeCompare(String(right[key] ?? '')));
  });

  constructor() {
    this.title.setTitle('Users - User Management');
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getUsers(this.userPageNumber(), this.userPageSize())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (users) => {
          this.users.set(users.data ?? []);
          this.userPagination.set(users.pagination);
        },
        error: (response) => this.error.set(response.error?.message || response.message || 'Unable to load user data.')
      });

    if (this.auth.isAdmin()) {
      this.api.getLoginAudits(this.auditPageNumber(), this.auditPageSize())
        .subscribe({
          next: (audits) => {
            this.audits.set(audits.data ?? []);
            this.auditPagination.set(audits.pagination);
          }
        });
    }
  }

  setSort(key: SortKey): void {
    this.sortKey.set(key);
  }

  selectUser(user: UserProfile): void {
    this.selectedUser.set(user);
    this.roleForm.patchValue({ userId: user.userId });
  }

  assignRole(): void {
    if (this.roleForm.invalid || !this.auth.isAdmin()) {
      return;
    }

    this.assigningRole.set(true);
    this.roleMessage.set(null);
    this.roleSuccess.set(false);
    const { userId, role } = this.roleForm.getRawValue();

    this.api
      .assignRole(userId, role)
      .pipe(finalize(() => this.assigningRole.set(false)))
      .subscribe({
        next: (response) => {
          this.roleSuccess.set(response.success);
          this.roleMessage.set(response.success ? response.message || `Assigned ${role} role.` : response.errors.join(', '));
        },
        error: (response) => this.roleMessage.set(response.error?.errors?.join(', ') || response.error?.message || 'Unable to assign role.')
      });
  }

  previousUsersPage(): void {
    this.userPageNumber.update((page) => Math.max(1, page - 1));
    this.reload();
  }

  nextUsersPage(): void {
    this.userPageNumber.update((page) => page + 1);
    this.reload();
  }

  changeUserPageSize(pageSize: string): void {
    this.userPageSize.set(Number(pageSize));
    this.userPageNumber.set(1);
    this.reload();
  }

  previousAuditPage(): void {
    this.auditPageNumber.update((page) => Math.max(1, page - 1));
    this.reload();
  }

  nextAuditPage(): void {
    this.auditPageNumber.update((page) => page + 1);
    this.reload();
  }

  changeAuditPageSize(pageSize: string): void {
    this.auditPageSize.set(Number(pageSize));
    this.auditPageNumber.set(1);
    this.reload();
  }

  exportCsv(): void {
    const rows = [
      ['UserId', 'Username', 'Email'],
      ...this.filteredUsers().map((user) => [user.userId, user.username, user.email])
    ];
    const csv = rows.map((row) => row.map((cell) => `"${String(cell).replaceAll('"', '""')}"`).join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'users.csv';
    link.click();
    URL.revokeObjectURL(url);
  }
}
