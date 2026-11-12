import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../api/api-config';
import { getAuthErrorMessage } from './auth-error';
import { AuthenticationResult } from './auth.models';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    window.sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.test/api/v1' },
      ],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    window.sessionStorage.clear();
  });

  it('stores the returned account session after successful sign-in', () => {
    auth.login({ email: 'manager@example.com', password: 'strong-password' }).subscribe();
    const request = http.expectOne('http://api.test/api/v1/auth/login');
    expect(request.request.body).toEqual({ email: 'manager@example.com', password: 'strong-password' });
    request.flush(authenticationResult('access-1'));

    expect(auth.currentSession()?.user.email).toBe('manager@example.com');
    expect(window.sessionStorage.getItem('ethiofleet.session')).toContain('access-1');
  });

  it('keeps an unconfigured email provider error explicit during registration', () => {
    let responseError: unknown;
    auth.register({
      organizationName: 'Example Fleet',
      userName: 'Fleet Manager',
      email: 'manager@example.com',
      password: 'strong-password',
    }).subscribe({ error: (error: unknown) => responseError = error });

    const request = http.expectOne('http://api.test/api/v1/auth/register');
    request.flush({ title: 'Email delivery is not configured.' }, {
      status: 503,
      statusText: 'Service Unavailable',
    });

    expect(responseError).toBeInstanceOf(HttpErrorResponse);
    expect(getAuthErrorMessage(responseError, 'register')).toContain('Registration was not completed');
    expect(auth.currentSession()).toBeNull();
  });
});

function authenticationResult(accessToken: string): AuthenticationResult {
  return {
    succeeded: true,
    tokens: {
      accessToken,
      accessTokenExpiresAt: '2026-11-12T16:30:00Z',
      refreshToken: `refresh-${accessToken}`,
      refreshTokenExpiresAt: '2026-11-26T16:15:00Z',
    },
    user: {
      id: 'user-id',
      organizationId: 'organization-id',
      email: 'manager@example.com',
      userName: 'Fleet Manager',
      roles: ['CompanyOwner'],
    },
    error: null,
  };
}
