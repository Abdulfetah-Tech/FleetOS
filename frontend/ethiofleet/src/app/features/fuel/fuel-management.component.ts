import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { getApiMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { FuelRecord, FuelRecordPage } from '../../core/fuel/fuel.models';
import { FuelService } from '../../core/fuel/fuel.service';
import { Vehicle, VehiclePage } from '../../core/vehicles/vehicle.models';
import { VehicleService } from '../../core/vehicles/vehicle.service';

@Component({
  selector: 'app-fuel-management',
  standalone: true,
  imports: [ReactiveFormsModule],
  host: { class: 'vehicle-management fuel-management' },
  templateUrl: './fuel-management.component.html',
  styleUrl: './fuel-management.component.scss',
})
export class FuelManagementComponent implements OnInit {
  private readonly fuel = inject(FuelService);
  private readonly vehicles = inject(VehicleService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly isCompanyOwner = computed(() => {
    const roles = this.auth.currentSession()?.user.roles ?? [];
    return roles.includes('CompanyOwner') || roles.includes('CompanyAdministrator');
  });
  protected readonly canRead = computed(() => this.isCompanyOwner() || (this.auth.currentSession()?.user.roles.includes('FleetManager') ?? false));
  protected readonly records = signal<FuelRecordPage | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly deleting = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly noticeMessage = signal<string | null>(null);
  protected readonly formOpen = signal(false);
  protected readonly page = signal(1);
  protected readonly pageSize = 20;
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil((this.records()?.totalCount ?? 0) / (this.records()?.pageSize ?? this.pageSize))));
  protected readonly filterVehicle = signal<Vehicle | null>(null);
  protected readonly createVehicle = signal<Vehicle | null>(null);
  protected readonly pickerMode = signal<'filter' | 'create' | null>(null);
  protected readonly vehiclePage = signal<VehiclePage | null>(null);
  protected readonly vehicleLoading = signal(false);
  protected readonly vehicleError = signal<string | null>(null);
  protected readonly vehicleSearch = new FormControl('', { nonNullable: true, validators: [Validators.maxLength(100)] });
  protected readonly vehiclePages = computed(() => Math.max(1, Math.ceil((this.vehiclePage()?.totalCount ?? 0) / (this.vehiclePage()?.pageSize ?? this.pageSize))));
  private requestedPage = 1;
  private requestedVehiclePage = 1;
  protected readonly form = new FormGroup({
    vehicleId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    occurredAt: new FormControl(localDateTimeInput(new Date()), { nonNullable: true, validators: [Validators.required, validLocalDateTime] }),
    quantityLiters: new FormControl<number | null>(null, [Validators.required, decimalValue(3, true)]),
    unitPrice: new FormControl<number | null>(null, [Validators.required, decimalValue(2)]),
    odometer: new FormControl<number | null>(null, [Validators.required, decimalValue(1)]),
    currencyCode: new FormControl('ETB', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)] }),
    station: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(160)] }),
    receiptReference: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(100)] }),
    notes: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(1000)] }),
  });

  ngOnInit(): void { this.loadRecords(); }

  protected loadRecords(page = this.requestedPage): void {
    if (!this.canRead() || this.loading()) return;
    this.requestedPage = page;
    this.loading.set(true);
    this.errorMessage.set(null);
    this.records.set(null);
    this.fuel.list(page, this.pageSize, this.filterVehicle()?.id ?? null)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => { this.records.set(result); this.page.set(result.page); },
        error: error => this.errorMessage.set(fuelError(error, 'Fuel records could not be loaded.')),
      });
  }

  protected goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages() && page !== this.page()) this.loadRecords(page);
  }

  protected openPicker(mode: 'filter' | 'create'): void {
    if (!this.canRead() || (mode === 'create' && !this.isCompanyOwner()) || this.loading() || this.saving() || this.deleting()) return;
    this.pickerMode.set(mode);
    this.vehicleSearch.setValue('');
    this.loadVehicles(1);
  }

  protected loadVehicles(page = this.requestedVehiclePage): void {
    if (!this.canRead() || !this.pickerMode() || this.vehicleLoading() || this.vehicleSearch.invalid || (this.pickerMode() === 'create' && !this.isCompanyOwner())) return;
    this.requestedVehiclePage = page;
    this.vehicleLoading.set(true);
    this.vehicleError.set(null);
    this.vehiclePage.set(null);
    this.vehicles.list(page, this.pageSize, this.vehicleSearch.value)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.vehicleLoading.set(false)))
      .subscribe({
        next: result => this.vehiclePage.set(result),
        error: error => this.vehicleError.set(fuelError(error, 'Vehicles could not be loaded.')),
      });
  }

  protected goToVehiclePage(page: number): void {
    if (page >= 1 && page <= this.vehiclePages() && page !== this.vehiclePage()?.page) this.loadVehicles(page);
  }

  protected selectVehicle(vehicle: Vehicle): void {
    if (!this.canRead() || this.loading() || this.saving() || this.vehicleLoading() || !this.vehiclePage()?.items.some(item => item.id === vehicle.id)) return;
    if (this.pickerMode() === 'create') {
      if (!this.isCompanyOwner()) return;
      this.createVehicle.set(vehicle);
      this.form.patchValue({ vehicleId: vehicle.id, odometer: vehicle.currentMileage });
      this.form.controls.odometer.setValidators([Validators.required, decimalValue(1), Validators.min(vehicle.currentMileage)]);
      this.form.controls.odometer.updateValueAndValidity();
    } else if (this.pickerMode() === 'filter') {
      this.filterVehicle.set(vehicle);
      this.loadRecords(1);
    } else return;
    this.pickerMode.set(null);
  }

  protected clearFilter(): void {
    if (!this.canRead() || this.loading()) return;
    this.filterVehicle.set(null);
    this.loadRecords(1);
  }

  protected beginCreate(): void {
    if (!this.isCompanyOwner() || this.loading() || this.saving() || this.deleting()) return;
    this.form.reset({ vehicleId: '', occurredAt: localDateTimeInput(new Date()), quantityLiters: null, unitPrice: null, odometer: null, currencyCode: 'ETB', station: '', receiptReference: '', notes: '' });
    this.createVehicle.set(null);
    this.formError.set(null);
    this.noticeMessage.set(null);
    this.formOpen.set(true);
    this.openPicker('create');
  }

  protected closeForm(): void {
    if (this.saving()) return;
    this.formOpen.set(false);
    if (this.pickerMode() === 'create') this.pickerMode.set(null);
  }

  protected createRecord(): void {
    if (!this.isCompanyOwner() || !this.formOpen() || this.saving() || this.deleting()) return;
    this.form.markAllAsTouched();
    this.formError.set(null);
    const value = this.form.getRawValue();
    if (this.form.invalid || !this.createVehicle() || value.vehicleId !== this.createVehicle()?.id) {
      this.formError.set('Choose a vehicle and correct the highlighted fields before saving.');
      return;
    }
    this.saving.set(true);
    this.fuel.create({
      vehicleId: value.vehicleId, driverId: null, tripId: null,
      occurredAt: new Date(value.occurredAt).toISOString(),
      quantityLiters: value.quantityLiters!, unitPrice: value.unitPrice!, odometer: value.odometer!,
      currencyCode: value.currencyCode.toUpperCase(), station: nullableTrimmed(value.station),
      receiptReference: nullableTrimmed(value.receiptReference), notes: nullableTrimmed(value.notes),
    }).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.saving.set(false))).subscribe({
      next: record => {
        this.formOpen.set(false);
        this.pickerMode.set(null);
        this.noticeMessage.set(`Fuel record for ${record.vehiclePlateNumber} was created.`);
        this.loadRecords(1);
      },
      error: error => this.formError.set(fuelError(error, 'The fuel record could not be created.')),
    });
  }

  protected deleteRecord(record: FuelRecord): void {
    if (!this.isCompanyOwner() || this.loading() || this.saving() || this.deleting() || !window.confirm(`Delete the fuel record for ${record.vehiclePlateNumber}? This cannot be undone.`)) return;
    this.errorMessage.set(null);
    this.noticeMessage.set(null);
    this.deleting.set(record.id);
    this.fuel.delete(record.id).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.deleting.set(null))).subscribe({
      next: () => {
        this.noticeMessage.set('Fuel record deleted.');
        this.loadRecords(this.records()?.items.length === 1 ? Math.max(1, this.page() - 1) : this.page());
      },
      error: error => this.errorMessage.set(fuelError(error, 'The fuel record could not be deleted.')),
    });
  }

  protected invalid(name: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[name];
    return control.invalid && control.touched;
  }

  protected formatDate(value: string): string {
    const date = new Date(value);
    return Number.isFinite(date.getTime()) ? new Intl.DateTimeFormat('en-ET', { dateStyle: 'medium', timeStyle: 'short' }).format(date) : '—';
  }

  protected formatNumber(value: number, decimals: number): string {
    return new Intl.NumberFormat('en-ET', { maximumFractionDigits: decimals }).format(value);
  }

  protected formatCost(value: number, currency: string): string {
    return `${currency} ${new Intl.NumberFormat('en-ET', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value)}`;
  }
}

function nullableTrimmed(value: string): string | null { return value.trim() || null; }

export function localDateTimeInput(value: Date): string {
  return new Date(value.getTime() - value.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
}

function validLocalDateTime(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value)) return { dateTime: true };
  const date = new Date(value);
  // Round-trip rejects impossible calendar dates and nonexistent local times (DST gaps).
  return Number.isFinite(date.getTime()) && localDateTimeInput(date) === value ? null : { dateTime: true };
}

function decimalValue(places: number, positive = false): ValidatorFn {
  return control => {
    const value = control.value;
    return typeof value === 'number' && Number.isFinite(value) && (positive ? value > 0 : value >= 0) && Math.round(value * 10 ** places) / 10 ** places === value ? null : { decimal: true };
  };
}

function fuelError(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'The fleet API could not be reached. Check the connection and try again.';
    if (error.status === 401) return 'Your session could not be renewed. Sign in again to continue.';
    if (error.status === 403) return 'Your account does not have permission to complete this fuel operation.';
    const errors = error.error?.errors;
    if (errors && typeof errors === 'object') {
      const messages = Object.values(errors).flat().filter((value): value is string => typeof value === 'string');
      if (messages.length) return messages.join(' ');
    }
  }
  return getApiMessage(error) ?? fallback;
}
