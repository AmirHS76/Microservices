import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgIcon } from '@ng-icons/core';
import { finalize, forkJoin } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ChatMessage, ChatUser, Conversation } from '../../core/models/api.models';
import { ChatApiService } from '../../core/models/chat-api.service';
import { ChatRealtimeService } from '../../core/models/chat-realtime.service';

@Component({
    selector: 'app-chat',
    imports: [ReactiveFormsModule, NgIcon, DatePipe],
    template: `
    <section class="chat-shell">
      <aside class="people-panel">
        <div class="panel-header">
          <div>
            <span class="eyebrow"><ng-icon name="lucideMessageCircle" size="14" /> Private messages</span>
            <h2>Chat</h2>
          </div>
          <span class="connection" [class.online]="realtime.connected()">
            <ng-icon [name]="realtime.connected() ? 'lucideWifi' : 'lucideWifiOff'" size="16" />
            {{ realtime.connected() ? 'Live' : 'Offline' }}
          </span>
        </div>

        <label class="search-box">
          <ng-icon name="lucideSearch" size="18" />
          <input type="search" [value]="query()" (input)="query.set($any($event.target).value)" placeholder="Search people" />
        </label>

        <div class="people-list">
          @for (user of filteredUsers(); track user.userId) {
            <button type="button" class="person-row" [class.active]="selectedUser()?.userId === user.userId" (click)="selectUser(user)">
              <span class="avatar">{{ initials(user) }}</span>
              <span>
                <strong>{{ user.username || user.email }}</strong>
                <small>{{ lastPreview(user.userId) }}</small>
              </span>
            </button>
          } @empty {
            <p class="empty-state">No chat contacts are available yet.</p>
          }
        </div>
      </aside>

      <section class="thread-panel">
        @if (selectedUser(); as user) {
          <header class="thread-header">
            <span class="avatar large">{{ initials(user) }}</span>
            <div>
              <h2>{{ user.username || user.email }}</h2>
              <span>{{ user.email }}</span>
            </div>
          </header>

          <div class="messages-frame" (scroll)="onScroll($event)">
            @if (loadingMessages()) {
              <p class="empty-state">Loading messages...</p>
            }

            @for (message of orderedMessages(); track message.id) {
              <article class="message" [class.mine]="message.senderId === currentUserId()">
                <p>{{ message.body }}</p>
                <footer>
                  <time>{{ message.createdAtUtc | date: 'shortTime' }}</time>
                  @if (message.senderId === currentUserId()) {
                    <span class="status">
                      <ng-icon [name]="message.status === 'Read' || message.status === 'Delivered' ? 'lucideCheckCheck' : 'lucideCheck'" size="14" />
                      {{ message.status }}
                    </span>
                  }
                </footer>
              </article>
            } @empty {
              <p class="empty-state">Start the conversation with a short message.</p>
            }
          </div>

          <form class="composer" [formGroup]="messageForm" (ngSubmit)="sendMessage()">
            <input formControlName="body" placeholder="Write a private message" />
            <button class="button primary" type="submit" [disabled]="messageForm.invalid || sending()">
              <ng-icon name="lucideSend" size="18" />
              Send
            </button>
          </form>
        } @else {
          <div class="empty-thread">
            <ng-icon name="lucideMessageCircle" size="42" />
            <h2>Select a conversation</h2>
            <p>Choose any authenticated user from the list to start a private chat.</p>
          </div>
        }
      </section>
    </section>
  `,
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './chat.component.scss'
})
export class ChatComponent {
  private readonly api = inject(ChatApiService);
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  readonly realtime = inject(ChatRealtimeService);
  readonly users = signal<ChatUser[]>([]);
  readonly conversations = signal<Conversation[]>([]);
  readonly messages = signal<ChatMessage[]>([]);
  readonly selectedUser = signal<ChatUser | null>(null);
  readonly loading = signal(false);
  readonly loadingMessages = signal(false);
  readonly sending = signal(false);
  readonly query = signal('');
  readonly pageNumber = signal(1);
  readonly pageSize = signal(30);
  readonly hasMore = signal(true);

  readonly messageForm = this.fb.nonNullable.group({
    body: ['', [Validators.required, Validators.maxLength(4000)]]
  });

  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? '');
  readonly filteredUsers = computed(() => {
    const term = this.query().trim().toLowerCase();
    return this.users().filter((user) => {
      const haystack = `${user.username} ${user.email}`.toLowerCase();
      return !term || haystack.includes(term);
    });
  });
  readonly orderedMessages = computed(() =>
    [...this.messages()].sort((left, right) => new Date(left.createdAtUtc).getTime() - new Date(right.createdAtUtc).getTime())
  );

  constructor() {
    this.loadInitial();
    void this.realtime.connect();

    effect(() => {
      const message = this.realtime.incoming();
      if (message) {
        this.upsertMessage(message);
        if (this.selectedUser()?.userId === message.senderId) {
          void this.realtime.markConversationRead(message.senderId);
        }
      }
    });

    effect(() => {
      const message = this.realtime.saved();
      if (message) {
        this.upsertMessage(message);
      }
    });

    effect(() => {
      const ids = this.realtime.deliveredIds();
      if (ids.length) {
        this.patchStatuses(ids, 'Delivered');
      }
    });

    effect(() => {
      const ids = this.realtime.readIds();
      if (ids.length) {
        this.patchStatuses(ids, 'Read');
      }
    });
  }

  loadInitial(): void {
    this.loading.set(true);
    forkJoin({
      users: this.api.getUsers(),
      conversations: this.api.getConversations()
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ users, conversations }) => {
        this.users.set(users.data ?? []);
        this.conversations.set(conversations.data ?? []);
      });
  }

  selectUser(user: ChatUser): void {
    this.selectedUser.set(user);
    this.messages.set([]);
    this.pageNumber.set(1);
    this.hasMore.set(true);
    this.loadMessages();
    void this.realtime.markConversationRead(user.userId);
  }

  loadMessages(): void {
    const user = this.selectedUser();
    if (!user || this.loadingMessages() || !this.hasMore()) {
      return;
    }

    this.loadingMessages.set(true);
    this.api
      .getMessages(user.userId, this.pageNumber(), this.pageSize())
      .pipe(finalize(() => this.loadingMessages.set(false)))
      .subscribe((response) => {
        const older = response.data ?? [];
        this.hasMore.set(older.length === this.pageSize());
        this.messages.update((messages) => this.mergeMessages([...messages, ...older]));
      });
  }

  onScroll(event: Event): void {
    const element = event.target as HTMLElement;
    if (element.scrollTop < 80 && this.hasMore() && !this.loadingMessages()) {
      this.pageNumber.update((page) => page + 1);
      this.loadMessages();
    }
  }

  sendMessage(): void {
    const user = this.selectedUser();
    if (!user || this.messageForm.invalid) {
      return;
    }

    const body = this.messageForm.getRawValue().body.trim();
    if (!body) {
      return;
    }

    this.sending.set(true);
    this.realtime
      .sendPrivateMessage(user.userId, body)
      .then(() => this.messageForm.reset())
      .finally(() => this.sending.set(false));
  }

  initials(user: ChatUser): string {
    return (user.username || user.email || 'U').slice(0, 2).toUpperCase();
  }

  lastPreview(userId: string): string {
    const conversation = this.conversations().find((item) => item.otherUserId === userId);
    return conversation?.lastMessage?.body || 'No messages yet';
  }

  private upsertMessage(message: ChatMessage): void {
    const active = this.selectedUser();
    const belongsToActive =
      active &&
      ((message.senderId === this.currentUserId() && message.recipientId === active.userId) ||
        (message.recipientId === this.currentUserId() && message.senderId === active.userId));

    if (belongsToActive) {
      this.messages.update((messages) => this.mergeMessages([...messages, message]));
    }
  }

  private patchStatuses(ids: string[], status: ChatMessage['status']): void {
    this.messages.update((messages) =>
      messages.map((message) => (ids.includes(message.id) ? { ...message, status } : message))
    );
  }

  private mergeMessages(messages: ChatMessage[]): ChatMessage[] {
    const map = new Map<string, ChatMessage>();
    for (const message of messages) {
      map.set(message.id, message);
    }

    return [...map.values()];
  }
}
