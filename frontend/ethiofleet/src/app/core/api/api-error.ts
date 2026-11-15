import { HttpErrorResponse } from '@angular/common/http';

export function getApiMessage(error: unknown, fallback?: string): string {
  if (!(error instanceof HttpErrorResponse) || typeof error.error !== 'object' || error.error === null) {
    return fallback ?? 'An unexpected error occurred.';
  }

  if ('error' in error.error && typeof error.error.error === 'string') {
    return error.error.error;
  }

  if ('detail' in error.error && typeof error.error.detail === 'string') {
    return error.error.detail;
  }

  return fallback ?? 'An unexpected error occurred.';
}
