import { HttpClient } from "@angular/common/http";
import { Injectable, computed, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Observable, tap } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  AuthResponse,
  BaseResponse,
  CurrentUser,
  RegisterResponse,
} from "../models/api.models";

const tokenKey = "microservices.userManagement.token";

@Injectable({ providedIn: "root" })
export class AuthService {
  private readonly token = signal<string | null>(
    localStorage.getItem(tokenKey),
  );
  readonly currentUser = computed(() => this.decodeToken(this.token()));
  readonly isAuthenticated = computed(() => {
    const user = this.currentUser();
    return !!this.token() && (!user?.expiresAt || user.expiresAt > Date.now());
  });
  readonly isAdmin = computed(
    () => this.currentUser()?.roles.includes("Admin") ?? false,
  );

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {}

  get accessToken(): string | null {
    return this.token();
  }

  login(
    email: string | null,
    username: string | null,
    password: string,
  ): Observable<BaseResponse<AuthResponse>> {
    return this.http
      .post<
        BaseResponse<AuthResponse>
      >(`${environment.apiBaseUrl}/sso/auth/login`, { email, username, password })
      .pipe(tap((response) => this.storeToken(response)));
  }

  register(
    username: string,
    email: string,
    password: string,
  ): Observable<BaseResponse<RegisterResponse>> {
    return this.http.post<BaseResponse<RegisterResponse>>(
      `${environment.apiBaseUrl}/register`,
      { username, email, password },
    );
  }

  logout(): void {
    localStorage.removeItem(tokenKey);
    this.token.set(null);
    void this.router.navigateByUrl("/login");
  }

  private storeToken(response: BaseResponse<AuthResponse>): void {
    if (!response.success || !response.data?.token) {
      return;
    }

    localStorage.setItem(tokenKey, response.data.token);
    this.token.set(response.data.token);
  }

  private decodeToken(token: string | null): CurrentUser | null {
    if (!token) {
      return null;
    }

    try {
      const payload = JSON.parse(atob(token.split(".")[1] ?? "")) as Record<
        string,
        unknown
      >;
      const roleClaim =
        payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
      const roles = Array.isArray(roleClaim)
        ? roleClaim.map(String)
        : roleClaim
          ? [String(roleClaim)]
          : [];
      const exp =
        typeof payload["exp"] === "number" ? payload["exp"] * 1000 : null;
      const id = String(
        payload["sub"] ??
          payload[
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
          ] ??
          "",
      );
      const email = String(
        payload["email"] ??
          payload[
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
          ] ??
          "",
      );

      return { id, email, roles, expiresAt: exp };
    } catch {
      return null;
    }
  }
}
