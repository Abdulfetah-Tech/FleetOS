import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { AuthService } from './auth.service';
import { SKIP_AUTH } from './auth-context';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const apiBaseUrl = inject(API_BASE_URL);
  if (request.context.get(SKIP_AUTH) || !request.url.startsWith(apiBaseUrl)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const router = inject(Router);
  const session = auth.currentSession();
  if (!session) {
    return next(request);
  }

  const withToken = (accessToken: string) => request.clone({
    setHeaders: { Authorization: `Bearer ${accessToken}` },
  });

  return next(withToken(session.tokens.accessToken)).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap(() => {
          const updatedSession = auth.currentSession();
          return updatedSession
            ? next(withToken(updatedSession.tokens.accessToken))
            : throwError(() => error);
        }),
        catchError((refreshError: unknown) => {
          auth.clearSession();
          void router.navigate(['/login'], { queryParams: { expired: 'true' } });
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
