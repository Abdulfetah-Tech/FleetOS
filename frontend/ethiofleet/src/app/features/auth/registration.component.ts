import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { getAuthErrorMessage } from '../../core/auth/auth-error';
import { RegistrationAcceptedResponse } from '../../core/auth/auth.models';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-registration',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './registration.component.html',
  styleUrl: './auth.component.scss',
})
export class RegistrationComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  protected readonly theme = inject(ThemeService);
  protected readonly pending = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly accepted = signal<RegistrationAcceptedResponse | null>(null);
  protected readonly form = this.formBuilder.nonNullable.group({
    organizationName: ['', Validators.required],
    userName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.pending()) {
      return;
    }

    this.pending.set(true);
    this.errorMessage.set(null);
    this.auth.register(this.form.getRawValue()).pipe(
      finalize(() => this.pending.set(false)),
    ).subscribe({
      next: (response) => this.accepted.set(response),
      error: (error: unknown) => this.errorMessage.set(getAuthErrorMessage(error, 'register')),
    });
  }
}
