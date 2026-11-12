import { HttpErrorResponse } from '@angular/common/http';
import { getApiMessage } from '../api/api-error';

export function getAuthErrorMessage(error: unknown, operation: 'login' | 'register'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return operation === 'login'
      ? 'Sign-in could not be completed. Try again.'
      : 'Registration could not be completed. Try again.';
  }

  if (error.status === 0) {
    return 'The EthioFleet API could not be reached. Check the server connection and try again.';
  }
  if (error.status === 503 && operation === 'register') {
    return 'Email delivery is not configured on this server. Registration was not completed; contact your administrator.';
  }
  if (error.status === 401 && operation === 'login') {
    return 'Sign-in was not accepted. Check your email and password; new accounts must confirm their email first.';
  }
  if (error.status === 429) {
    return 'Too many account requests were made. Wait a moment and try again.';
  }
  if (error.status === 400) {
    return getApiMessage(error) ?? 'Check the account details and try again.';
  }

  return operation === 'login'
    ? 'Sign-in could not be completed. Try again.'
    : 'Registration could not be completed. Try again.';
}
