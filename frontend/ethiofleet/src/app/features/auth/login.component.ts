import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { getAuthErrorMessage } from '../../core/auth/auth-error';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './auth.component.scss',
})
export class LoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly theme = inject(ThemeService);
  protected readonly pending = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  constructor() {
    if (this.route.snapshot.queryParamMap.has('expired')) {
      this.notice.set('Your session expired. Sign in again to continue.');
    } else if (this.route.snapshot.queryParamMap.has('logout')) {
      this.notice.set('You signed out on this device, but server revocation could not be confirmed.');
    }
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.pending()) {
      return;
    }

    this.pending.set(true);
    this.errorMessage.set(null);
    this.auth.login(this.form.getRawValue()).pipe(
      finalize(() => this.pending.set(false)),
    ).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        const destination = returnUrl?.startsWith('/') && !returnUrl.startsWith('//')
          ? returnUrl
          : '/dashboard';
        void this.router.navigateByUrl(destination);
      },
      error: (error: unknown) => this.errorMessage.set(getAuthErrorMessage(error, 'login')),
    });
  }
}
