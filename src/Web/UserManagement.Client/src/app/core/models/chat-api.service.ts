import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BaseResponse, ChatMessage, ChatUser, Conversation } from './api.models';

@Injectable({ providedIn: 'root' })
export class ChatApiService {
  constructor(private readonly http: HttpClient) {}

  getUsers(): Observable<BaseResponse<ChatUser[]>> {
    return this.http.get<BaseResponse<ChatUser[]>>(`${environment.apiBaseUrl}/chat/users`);
  }

  getConversations(): Observable<BaseResponse<Conversation[]>> {
    return this.http.get<BaseResponse<Conversation[]>>(`${environment.apiBaseUrl}/chat/conversations`);
  }

  getMessages(otherUserId: string, pageNumber: number, pageSize: number): Observable<BaseResponse<ChatMessage[]>> {
    return this.http.get<BaseResponse<ChatMessage[]>>(`${environment.apiBaseUrl}/chat/messages/${otherUserId}`, {
      params: {
        pageNumber,
        pageSize
      }
    });
  }
}
