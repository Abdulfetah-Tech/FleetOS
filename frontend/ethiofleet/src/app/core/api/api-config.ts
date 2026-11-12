import { InjectionToken } from '@angular/core';

export interface EthioFleetRuntimeConfig {
  apiBaseUrl?: string;
}

declare global {
  interface Window {
    __ETHIOFLEET_CONFIG__?: EthioFleetRuntimeConfig;
  }
}

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => {
    const configured = typeof window === 'undefined'
      ? undefined
      : window.__ETHIOFLEET_CONFIG__?.apiBaseUrl?.trim();
    return (configured || 'http://localhost:5062/api/v1').replace(/\/+$/, '');
  },
});
