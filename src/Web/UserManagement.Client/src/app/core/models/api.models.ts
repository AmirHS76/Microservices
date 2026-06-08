export interface AuthResponse {
  token: string;
}

export interface RegisterResponse {
  userId: string;
  message: string;
}

export interface OperationResponse {
  success?: boolean;
}

export interface BaseResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: string[];
  pagination: PaginationMetadata | null;
}

export interface PaginationMetadata {
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UserProfile {
  userId: string;
  username: string;
  email: string;
}

export interface LoginAudit {
  userId: string;
  username: string;
  occurredAtUtc: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  roles: string[];
  expiresAt: number | null;
}

export interface ChatUser {
  userId: string;
  username: string;
  email: string;
}

export type ChatMessageStatus = 'Pending' | 'Sent' | 'Delivered' | 'Read';

export interface ChatMessage {
  id: string;
  senderId: string;
  recipientId: string;
  body: string;
  status: ChatMessageStatus;
  createdAtUtc: string;
  deliveredAtUtc: string | null;
  readAtUtc: string | null;
}

export interface Conversation {
  id: string;
  otherUserId: string;
  otherUsername: string;
  otherEmail: string;
  lastMessage: ChatMessage | null;
  lastMessageAtUtc: string;
}
