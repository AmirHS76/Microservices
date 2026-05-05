import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BaseResponse, LoginAudit, OperationResponse, UserProfile } from './api.models';

@Injectable({ providedIn: 'root' })
export class UserApiService {
  constructor(private readonly http: HttpClient) {}

  getUsers(pageNumber: number, pageSize: number): Observable<BaseResponse<UserProfile[]>> {
    return this.http.get<BaseResponse<UserProfile[]>>(`${environment.apiBaseUrl}/user`, {
      params: {
        pageNumber,
        pageSize
      }
    });
  }

  getLoginAudits(pageNumber: number, pageSize: number): Observable<BaseResponse<LoginAudit[]>> {
    return this.http.get<BaseResponse<LoginAudit[]>>(`${environment.apiBaseUrl}/sso/audits`, {
      params: {
        pageNumber,
        pageSize
      }
    });
  }

  assignRole(userId: string, role: string): Observable<BaseResponse<OperationResponse>> {
    return this.http.post<BaseResponse<OperationResponse>>(`${environment.apiBaseUrl}/sso/auth/assign-role`, { userId, role });
  }

  updateUser(userId: string, username: string, email: string): Observable<BaseResponse<OperationResponse>> {
    return this.http.put<BaseResponse<OperationResponse>>(`${environment.apiBaseUrl}/user/${userId}`, { username, email });
  }
}
