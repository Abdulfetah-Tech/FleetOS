import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { API_BASE_URL } from '../api/api-config';
import { AuthenticationResult } from './auth.models';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let auth: AuthService;

  beforeEach(() => {
    window.sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.test/api/v1' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => {
    http.verify();
    window.sessionStorage.clear();
  });

  it('refreshes an expired access token once and retries the protected request', () => {
    auth.login({ email: 'manager@example.com', password: 'strong-password' }).subscribe();
    http.expectOne('http://api.test/api/v1/auth/login').flush(authenticationResult('access-1'));

    let receivedResponse = false;
    client.get('http://api.test/api/v1/dashboard/fleet').subscribe(() => receivedResponse = true);
    const expiredRequest = http.expectOne('http://api.test/api/v1/dashboard/fleet');
    expect(expiredRequest.request.headers.get('Authorization')).toBe('Bearer access-1');
    expiredRequest.flush(null, { status: 401, statusText: 'Unauthorized' });

    const refreshRequest = http.expectOne('http://api.test/api/v1/auth/refresh');
    expect(refreshRequest.request.body).toEqual({ refreshToken: 'refresh-access-1' });
    refreshRequest.flush(authenticationResult('access-2'));

    const retriedRequest = http.expectOne('http://api.test/api/v1/dashboard/fleet');
    expect(retriedRequest.request.headers.get('Authorization')).toBe('Bearer access-2');
    retriedRequest.flush({ totalVehicles: 1 });

    expect(receivedResponse).toBe(true);
    expect(auth.currentSession()?.tokens.accessToken).toBe('access-2');
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
