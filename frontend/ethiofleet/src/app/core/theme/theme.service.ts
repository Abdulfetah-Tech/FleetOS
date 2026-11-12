import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'ethiofleet.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly themeState = signal<Theme>(readStoredTheme());

  readonly theme = this.themeState.asReadonly();

  constructor() {
    this.applyTheme(this.themeState());
  }

  toggle(): void {
    const nextTheme = this.themeState() === 'light' ? 'dark' : 'light';
    this.themeState.set(nextTheme);
    this.applyTheme(nextTheme);
    if (typeof window !== 'undefined') {
      window.sessionStorage.setItem(THEME_STORAGE_KEY, nextTheme);
    }
  }

  private applyTheme(theme: Theme): void {
    this.document.documentElement.setAttribute('data-theme', theme);
  }
}

function readStoredTheme(): Theme {
  if (typeof window === 'undefined') {
    return 'light';
  }
  return window.sessionStorage.getItem(THEME_STORAGE_KEY) === 'dark' ? 'dark' : 'light';
}
