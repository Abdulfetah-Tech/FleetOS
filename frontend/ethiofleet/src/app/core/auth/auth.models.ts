export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest extends LoginRequest {
  userName: string;
  organizationName: string;
}

export interface AuthenticationTokens {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface AuthenticatedUser {
  id: string;
  organizationId: string;
  email: string;
  userName: string;
  roles: string[];
}

export interface AuthenticationResult {
  succeeded: boolean;
  tokens: AuthenticationTokens | null;
  user: AuthenticatedUser | null;
  error: string | null;
}

export interface AuthSession {
  tokens: AuthenticationTokens;
  user: AuthenticatedUser;
}

export interface RegistrationAcceptedResponse {
  message: string;
}

export function isAuthSession(value: unknown): value is AuthSession {
  if (typeof value !== 'object' || value === null || !('tokens' in value) || !('user' in value)) {
    return false;
  }

  const tokens = value.tokens;
  const user = value.user;
  return typeof tokens === 'object'
    && tokens !== null
    && 'accessToken' in tokens
    && typeof tokens.accessToken === 'string'
    && 'refreshToken' in tokens
    && typeof tokens.refreshToken === 'string'
    && typeof user === 'object'
    && user !== null
    && 'email' in user
    && typeof user.email === 'string'
    && 'roles' in user
    && Array.isArray(user.roles)
    && user.roles.every((role: unknown) => typeof role === 'string');
}
