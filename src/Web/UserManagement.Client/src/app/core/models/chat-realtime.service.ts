import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { ChatMessage } from './api.models';

@Injectable({ providedIn: 'root' })
export class ChatRealtimeService {
  private readonly auth = inject(AuthService);
  private connection: signalR.HubConnection | null = null;
  readonly connected = signal(false);
  readonly incoming = signal<ChatMessage | null>(null);
  readonly saved = signal<ChatMessage | null>(null);
  readonly deliveredIds = signal<string[]>([]);
  readonly readIds = signal<string[]>([]);

  async connect(): Promise<void> {
    if (this.connection || !this.auth.accessToken) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/chatHub`, {
        accessTokenFactory: () => this.auth.accessToken ?? ''
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('MessageQueued', (message: ChatMessage) => this.saved.set(message));
    this.connection.on('MessageSaved', (message: ChatMessage) => this.saved.set(message));
    this.connection.on('ReceiveMessage', (message: ChatMessage) => this.incoming.set(message));
    this.connection.on('MessagesDelivered', (ids: string[]) => this.deliveredIds.set(ids));
    this.connection.on('MessagesRead', (ids: string[]) => this.readIds.set(ids));
    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onclose(() => {
      this.connected.set(false);
      this.connection = null;
    });

    try {
      await this.connection.start();
      this.connected.set(true);
    } catch {
      this.connected.set(false);
      this.connection = null;
    }
  }

  async disconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    await this.connection.stop();
    this.connection = null;
    this.connected.set(false);
  }

  async sendPrivateMessage(recipientId: string, body: string): Promise<void> {
    await this.connect();
    await this.connection?.invoke('SendPrivateMessage', {
      recipientId,
      body,
      clientMessageId: crypto.randomUUID()
    });
  }

  async markConversationRead(otherUserId: string): Promise<void> {
    await this.connect();
    await this.connection?.invoke('MarkConversationRead', otherUserId);
  }

  clearStale(): void {
    this.incoming.set(null);
    this.saved.set(null);
    this.deliveredIds.set([]);
    this.readIds.set([]);
  }
}
