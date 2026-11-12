import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { getApiMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { CreateDriverRequest, DRIVER_STATUSES, Driver, DriverPage, DriverStatus, UpdateDriverRequest } from '../../core/drivers/driver.models';
import { DriverService } from '../../core/drivers/driver.service';

interface DriverForm {
  fullName: string;
  email: string;
  phoneNumber: string;
  licenseNumber: string;
  licenseCategory: string;
  licenseExpiryDate: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
}

@Component({
  selector: 'app-driver-management',
  standalone: true,
  imports: [FormsModule],
  host: { class: 'driver-management' },
  templateUrl: './driver-management.component.html',
})
export class DriverManagementComponent implements OnInit {
  private readonly driverService = inject(DriverService);
  private readonly auth = inject(AuthService);

  protected readonly driverPage = signal<DriverPage | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly noticeMessage = signal<string | null>(null);
  protected readonly formOpen = signal(false);
  protected readonly editingDriverId = signal<string | null>(null);
  protected readonly busyDriverId = signal<string | null>(null);
  protected readonly statusDrafts = signal<Record<string, DriverStatus>>({});
  protected readonly searchText = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = 20;
  protected readonly statuses = DRIVER_STATUSES;
  protected readonly isCompanyOwner = computed(
    () => this.auth.currentSession()?.user.roles.includes('CompanyOwner') ?? false,
  );
  protected readonly drivers = computed(() => this.driverPage()?.items ?? []);
  protected readonly totalPages = computed(
    () => Math.max(1, Math.ceil((this.driverPage()?.totalCount ?? 0) / this.pageSize)),
  );

  protected form: DriverForm = emptyDriverForm();

  ngOnInit(): void {
    this.loadDrivers();
  }

  protected loadDrivers(page = this.page()): void {
    if (this.loading()) {
      return;
    }
    this.loading.set(true);
    this.errorMessage.set(null);
    this.driverService.list(page, this.pageSize, this.searchText()).pipe(
      finalize(() => this.loading.set(false)),
    ).subscribe({
      next: (result) => {
        this.driverPage.set(result);
        this.page.set(result.page);
        this.statusDrafts.set({});
      },
      error: (error: unknown) => this.errorMessage.set(getDriverErrorMessage(error, 'Driver records could not be loaded.')),
    });
  }

  protected searchDrivers(): void {
    if (this.loading()) {
      return;
    }
    this.page.set(1);
    this.loadDrivers(1);
  }

  protected goToPage(page: number): void {
    if (this.loading() || page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.loadDrivers(page);
  }

  protected beginCreate(): void {
    this.editingDriverId.set(null);
    this.form = emptyDriverForm();
    this.formError.set(null);
    this.noticeMessage.set(null);
    this.formOpen.set(true);
  }

  protected beginEdit(driver: Driver): void {
    this.editingDriverId.set(driver.id);
    this.form = {
      fullName: driver.fullName,
      email: driver.email,
      phoneNumber: driver.phoneNumber,
      licenseNumber: driver.licenseNumber,
      licenseCategory: driver.licenseCategory,
      licenseExpiryDate: driver.licenseExpiryDate,
      emergencyContactName: driver.emergencyContactName ?? '',
      emergencyContactPhone: driver.emergencyContactPhone ?? '',
    };
    this.formError.set(null);
    this.noticeMessage.set(null);
    this.formOpen.set(true);
  }

  protected closeForm(): void {
    if (this.saving()) {
      return;
    }
    this.formOpen.set(false);
    this.formError.set(null);
  }

  protected saveDriver(): void {
    if (this.loading() || this.saving()) {
      return;
    }

    this.formError.set(null);
    const driverId = this.editingDriverId();
    const request: CreateDriverRequest = {
      fullName: this.form.fullName.trim(),
      email: this.form.email.trim(),
      phoneNumber: this.form.phoneNumber.trim(),
      licenseNumber: this.form.licenseNumber.trim(),
      licenseCategory: this.form.licenseCategory.trim(),
      licenseExpiryDate: this.form.licenseExpiryDate,
      emergencyContactName: nullableTrimmed(this.form.emergencyContactName),
      emergencyContactPhone: nullableTrimmed(this.form.emergencyContactPhone),
    };
    this.saving.set(true);
    const saveRequest = driverId
      ? this.driverService.update(driverId, request satisfies UpdateDriverRequest)
      : this.driverService.create(request);

    saveRequest.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.formOpen.set(false);
        this.noticeMessage.set(driverId ? 'Driver details updated.' : 'Driver added to the organization.');
        this.page.set(driverId ? this.page() : 1);
        this.loadDrivers(driverId ? this.page() : 1);
      },
      error: (error: unknown) => this.formError.set(getDriverErrorMessage(error, 'Driver changes could not be saved.')),
    });
  }

  protected statusDraft(driver: Driver): DriverStatus {
    return this.statusDrafts()[driver.id] ?? driver.status;
  }

  protected setStatusDraft(driver: Driver, value: string): void {
    const status = parseDriverStatus(value);
    if (!status) {
      this.errorMessage.set('The selected driver status is invalid.');
      return;
    }
    this.statusDrafts.update((drafts) => ({ ...drafts, [driver.id]: status }));
  }

  protected updateStatus(driver: Driver): void {
    const status = this.statusDraft(driver);
    if (this.loading() || status === driver.status || this.busyDriverId()) {
      return;
    }

    this.errorMessage.set(null);
    this.noticeMessage.set(null);
    this.busyDriverId.set(driver.id);
    this.driverService.changeStatus(driver.id, status).pipe(
      finalize(() => this.busyDriverId.set(null)),
    ).subscribe({
      next: () => {
        this.statusDrafts.update((drafts) => ({ ...drafts, [driver.id]: status }));
        this.noticeMessage.set(`Status updated for ${driver.fullName}.`);
        this.loadDrivers();
      },
      error: (error: unknown) => this.errorMessage.set(getDriverErrorMessage(error, 'Driver status could not be changed.')),
    });
  }

  protected deleteDriver(driver: Driver): void {
    if (!this.isCompanyOwner() || this.loading() || this.busyDriverId() ||
      !window.confirm(`Remove ${driver.fullName} from this organization?`)) {
      return;
    }

    this.errorMessage.set(null);
    this.noticeMessage.set(null);
    this.busyDriverId.set(driver.id);
    this.driverService.delete(driver.id).pipe(
      finalize(() => this.busyDriverId.set(null)),
    ).subscribe({
      next: () => {
        this.noticeMessage.set(`${driver.fullName} was removed from the organization.`);
        const remainingCount = (this.driverPage()?.totalCount ?? 1) - 1;
        const lastPage = Math.max(1, Math.ceil(remainingCount / this.pageSize));
        this.page.set(Math.min(this.page(), lastPage));
        this.loadDrivers(this.page());
      },
      error: (error: unknown) => this.errorMessage.set(getDriverErrorMessage(error, 'Driver could not be removed.')),
    });
  }

  protected statusLabel(status: DriverStatus): string {
    return DRIVER_STATUSES.find((item) => item.value === status)?.label ?? 'Unknown';
  }

  protected formatLicenseExpiry(value: string): string {
    const date = new Date(`${value}T00:00:00Z`);
    return Number.isNaN(date.getTime())
      ? value
      : new Intl.DateTimeFormat('en-ET', { dateStyle: 'medium', timeZone: 'UTC' }).format(date);
  }
}

function emptyDriverForm(): DriverForm {
  return {
    fullName: '',
    email: '',
    phoneNumber: '',
    licenseNumber: '',
    licenseCategory: '',
    licenseExpiryDate: '',
    emergencyContactName: '',
    emergencyContactPhone: '',
  };
}

function nullableTrimmed(value: string): string | null {
  const trimmed = value.trim();
  return trimmed || null;
}

function parseDriverStatus(value: string): DriverStatus | undefined {
  const parsed = Number(value);
  return DRIVER_STATUSES.find((item) => item.value === parsed)?.value;
}

function getDriverErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse && error.status === 0) {
    return 'The fleet API could not be reached. Check the server connection, then try again.';
  }
  if (error instanceof HttpErrorResponse && error.status === 401) {
    return 'Your session could not be renewed. Sign in again to continue.';
  }
  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'Your account does not have permission to complete this driver operation.';
  }
  return getApiMessage(error) ?? fallback;
}
