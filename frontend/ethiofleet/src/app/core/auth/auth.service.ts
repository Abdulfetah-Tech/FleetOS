import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, finalize, map, of, shareReplay, tap, throwError } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { anonymousRequestContext } from './auth-context';
import {
  AuthSession,
  AuthenticationResult,
  LoginRequest,
  RegisterRequest,
  RegistrationAcceptedResponse,
  isAuthSession,
} from './auth.models';

const SESSION_STORAGE_KEY = 'ethiofleet.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);
  private readonly sessionState = signal<AuthSession | null>(readStoredSession());
  private refreshInFlight: Observable<AuthSession> | undefined;

  readonly session = this.sessionState.asReadonly();

  currentSession(): AuthSession | null {
    return this.sessionState();
  }

  login(request: LoginRequest): Observable<AuthSession> {
    return this.http.post<AuthenticationResult>(`${this.apiBaseUrl}/auth/login`, request, {
      context: anonymousRequestContext(),
    }).pipe(
      map((result) => createSession(result)),
      tap((session) => this.storeSession(session)),
    );
  }

  register(request: RegisterRequest): Observable<RegistrationAcceptedResponse> {
    return this.http.post<RegistrationAcceptedResponse>(`${this.apiBaseUrl}/auth/register`, request, {
      context: anonymousRequestContext(),
    });
  }

  refresh(): Observable<AuthSession> {
    const session = this.sessionState();
    if (!session) {
      return throwError(() => new Error('No active session is available to refresh.'));
    }

    if (!this.refreshInFlight) {
      this.refreshInFlight = this.http.post<AuthenticationResult>(
        `${this.apiBaseUrl}/auth/refresh`,
        { refreshToken: session.tokens.refreshToken },
        { context: anonymousRequestContext() },
      ).pipe(
        map((result) => createSession(result)),
        tap((updatedSession) => this.storeSession(updatedSession)),
        finalize(() => {
          this.refreshInFlight = undefined;
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    }

    return this.refreshInFlight;
  }

  logout(): Observable<void> {
    const session = this.sessionState();
    if (!session) {
      return of(void 0);
    }

    return this.http.post<void>(
      `${this.apiBaseUrl}/auth/logout`,
      { refreshToken: session.tokens.refreshToken },
      {
        context: anonymousRequestContext(),
        headers: new HttpHeaders({ Authorization: `Bearer ${session.tokens.accessToken}` }),
      },
    ).pipe(finalize(() => this.clearSession()));
  }

  clearSession(): void {
    this.sessionState.set(null);
    if (typeof window !== 'undefined') {
      window.sessionStorage.removeItem(SESSION_STORAGE_KEY);
    }
  }

  private storeSession(session: AuthSession): void {
    this.sessionState.set(session);
    if (typeof window !== 'undefined') {
      window.sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
    }
  }
}

function createSession(result: AuthenticationResult): AuthSession {
  if (!result.succeeded || !result.tokens || !result.user) {
    throw new Error(result.error || 'The server did not return a valid authentication session.');
  }
  return { tokens: result.tokens, user: result.user };
}

function readStoredSession(): AuthSession | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const stored = window.sessionStorage.getItem(SESSION_STORAGE_KEY);
  if (!stored) {
    return null;
  }

  try {
    const value: unknown = JSON.parse(stored);
    if (isAuthSession(value)) {
      return value;
    }
  } catch (error) {
    if (!(error instanceof SyntaxError)) {
      throw error;
    }
  }

  window.sessionStorage.removeItem(SESSION_STORAGE_KEY);
  return null;
}
