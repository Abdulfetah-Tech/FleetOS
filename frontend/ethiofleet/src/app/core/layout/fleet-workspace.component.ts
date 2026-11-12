import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { ThemeService } from '../theme/theme.service';

@Component({
  selector: 'app-fleet-workspace',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  host: { class: 'fleet-workspace' },
  templateUrl: './fleet-workspace.component.html',
  styleUrl: './fleet-workspace.component.scss',
})
export class FleetWorkspaceComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly theme = inject(ThemeService);
  protected readonly signingOut = signal(false);

  protected get authSessionEmail(): string {
    return this.auth.currentSession()?.user.email ?? '';
  }

  protected signOut(): void {
    if (this.signingOut()) {
      return;
    }
    this.signingOut.set(true);
    this.auth.logout().subscribe({
      next: () => void this.router.navigateByUrl('/login'),
      error: () => void this.router.navigate(['/login'], { queryParams: { logout: 'unconfirmed' } }),
    });
  }
}
