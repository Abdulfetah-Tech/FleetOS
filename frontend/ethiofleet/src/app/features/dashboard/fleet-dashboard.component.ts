import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/theme/theme.service';
import { DueMaintenanceSchedule, FleetDashboardResponse } from '../../core/dashboard/fleet-dashboard.models';
import { FleetDashboardService } from '../../core/dashboard/fleet-dashboard.service';

@Component({
  selector: 'app-fleet-dashboard',
  standalone: true,
  imports: [RouterLink],
  host: { class: 'fleet-dashboard' },
  templateUrl: './fleet-dashboard.component.html',
  styleUrl: './fleet-dashboard.component.scss',
})
export class FleetDashboardComponent implements OnInit {
  private readonly dashboardService = inject(FleetDashboardService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly theme = inject(ThemeService);
  protected readonly dashboard = signal<FleetDashboardResponse | null>(null);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly signingOut = signal(false);
  protected readonly bandFlap = signal(false);
  protected readonly attentionCount = computed(() => {
    const data = this.dashboard();
    return data ? data.dueMaintenance.length + data.upcomingDocumentExpiries.length : 0;
  });

  ngOnInit(): void {
    this.loadDashboard();
  }

  protected get authSessionEmail(): string {
    return this.auth.currentSession()?.user.email ?? '';
  }

  protected loadDashboard(): void {
    if (this.loading()) {
      return;
    }
    this.loading.set(true);
    this.errorMessage.set(null);
    this.bandFlap.set(false);
    this.dashboardService.getFleetDashboard().pipe(
      finalize(() => this.loading.set(false)),
    ).subscribe({
      next: (response) => {
        this.dashboard.set(response);
        this.bandFlap.set(true);
      },
      error: (error: unknown) => this.errorMessage.set(getDashboardErrorMessage(error)),
    });
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

  protected formatDate(value: string): string {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
    if (!match) {
      return value;
    }
    return new Intl.DateTimeFormat('en-ET', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    }).format(new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3])));
  }

  protected formatNumber(value: number): string {
    return new Intl.NumberFormat('en-ET').format(value);
  }

  protected formatMoney(value: number, currencyCode: string): string {
    return `${currencyCode} ${new Intl.NumberFormat('en-ET', { maximumFractionDigits: 2 }).format(value)}`;
  }

  protected formatMileage(value: number): string {
    return `${this.formatNumber(value)} km`;
  }

  protected maintenanceContext(schedule: DueMaintenanceSchedule): string {
    const details: string[] = [];
    if (schedule.dueDate) {
      details.push(`Due ${this.formatDate(schedule.dueDate)}`);
    }
    if (schedule.dueMileage !== null) {
      details.push(`${this.formatMileage(schedule.currentMileage)} current / ${this.formatMileage(schedule.dueMileage)} due`);
    }
    return details.length > 0 ? details.join(' · ') : 'Maintenance threshold reached';
  }

  protected expiryLabel(daysUntilExpiry: number): string {
    if (daysUntilExpiry < 0) {
      const days = Math.abs(daysUntilExpiry);
      return `Expired ${days} ${days === 1 ? 'day' : 'days'} ago`;
    }
    if (daysUntilExpiry === 0) {
      return 'Expires today';
    }
    if (daysUntilExpiry === 1) {
      return 'Expires tomorrow';
    }
    return `Expires in ${daysUntilExpiry} days`;
  }
}

function getDashboardErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 0) {
    return 'The fleet API could not be reached. Check the server connection, then try again.';
  }
  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'Your account does not have permission to view this fleet dashboard.';
  }
  if (error instanceof HttpErrorResponse && error.status === 401) {
    return 'Your session could not be renewed. Sign in again to continue.';
  }
  return getApiMessage(error) ?? 'Fleet data could not be loaded. Try again.';
}
