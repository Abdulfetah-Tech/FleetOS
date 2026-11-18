import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { getApiMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { VehicleAssignmentService } from '../../core/assignments/vehicle-assignment.service';
import { Vehicle, VehiclePage } from '../../core/vehicles/vehicle.models';
import { VehicleService } from '../../core/vehicles/vehicle.service';
import { CreateFuelRecordRequest, FuelRecord, FuelRecordPage } from '../../core/fuel/fuel.models';
import { FuelService } from '../../core/fuel/fuel.service';

interface FuelForm {
  occurredAt: string;
  quantityLiters: number;
  unitPrice: number;
  odometer: number;
  station: string;
  receiptReference: string;
  notes: string;
}

@Component({
  selector: 'app-fuel-management',
  standalone: true,
  imports: [FormsModule],
  host: { class: 'fuel-management' },
  templateUrl: './fuel-management.component.html',
})
export class FuelManagementComponent implements OnInit {
  private readonly fuelService = inject(FuelService);
  private readonly vehicleService = inject(VehicleService);
  private readonly assignmentService = inject(VehicleAssignmentService);
  private readonly auth = inject(AuthService);

  protected readonly fuelPage = signal<FuelRecordPage | null>(null);
  protected readonly vehiclePage = signal<VehiclePage | null>(null);
  protected readonly loading = signal(false);
  protected readonly vehicleChoicesLoading = signal(false);
  protected readonly saving = signal(false);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly noticeMessage = signal<string | null>(null);
  protected readonly formOpen = signal(false);
  protected readonly selectedVehicle = signal<Vehicle | null>(null);
  protected readonly vehicleSearchText = signal('');
  protected readonly filterVehicleId = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);
  protected chosenVehicleId = '';

  protected readonly form: FuelForm = {
    occurredAt: localDateTimeValue(),
    quantityLiters: 50,
    unitPrice: 85,
    odometer: 0,
    station: '',
    receiptReference: '',
    notes: '',
  };

  protected readonly isCompanyOwner = computed(() => this.auth.isCompanyOwner());
  protected readonly records = computed(() => this.fuelPage()?.items ?? []);
  protected readonly computedTotalCost = computed(() => {
    const qty = Number(this.form.quantityLiters) || 0;
    const price = Number(this.form.unitPrice) || 0;
    return Math.round(qty * price * 100) / 100;
  });
  protected readonly totalCostCurrentPage = computed(() =>
    this.records().reduce((sum, item) => sum + (Number(item.totalCost) || 0), 0),
  );
  protected readonly totalLitersCurrentPage = computed(() =>
    this.records().reduce((sum, item) => sum + (Number(item.quantityLiters) || 0), 0),
  );
  protected readonly averagePricePerLiter = computed(() => {
    const liters = this.totalLitersCurrentPage();
    return liters > 0 ? this.totalCostCurrentPage() / liters : 0;
  });
  protected readonly totalPages = computed(() => {
    const page = this.fuelPage();
    return page ? Math.max(1, Math.ceil(page.totalCount / page.pageSize)) : 1;
  });

  ngOnInit(): void {
    this.loadFuelRecords();
  }

  loadFuelRecords(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.fuelService
      .list(this.page(), this.pageSize(), this.filterVehicleId() || null, null)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (page) => this.fuelPage.set(page),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(getApiMessage(error, 'Failed to load fuel records.')),
      });
  }

  setVehicleFilter(vehicleId: string): void {
    this.filterVehicleId.set(vehicleId);
    this.page.set(1);
    this.loadFuelRecords();
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((value) => value + 1);
      this.loadFuelRecords();
    }
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update((value) => value - 1);
      this.loadFuelRecords();
    }
  }

  beginCreate(): void {
    if (!this.isCompanyOwner()) {
      return;
    }
    this.formOpen.set(true);
    this.formError.set(null);
    this.noticeMessage.set(null);
    this.selectedVehicle.set(null);
    this.chosenVehicleId = '';
    this.form.occurredAt = localDateTimeValue();
    this.form.quantityLiters = 50;
    this.form.unitPrice = 85;
    this.form.odometer = 0;
    this.form.station = '';
    this.form.receiptReference = '';
    this.form.notes = '';
    this.searchVehicles();
  }

  closeForm(): void {
    this.formOpen.set(false);
    this.formError.set(null);
    this.selectedVehicle.set(null);
  }

  searchVehicles(): void {
    this.vehicleChoicesLoading.set(true);
    this.vehicleService
      .list(1, 25, this.vehicleSearchText().trim())
      .pipe(finalize(() => this.vehicleChoicesLoading.set(false)))
      .subscribe({
        next: (page) => this.vehiclePage.set(page),
        error: (error: HttpErrorResponse) =>
          this.formError.set(getApiMessage(error, 'Could not load vehicle candidates.')),
      });
  }

  onVehicleSelected(vehicleId: string): void {
    this.chosenVehicleId = vehicleId;
    const vehicle = this.vehiclePage()?.items.find((item) => item.id === vehicleId) ?? null;
    this.selectedVehicle.set(vehicle);
    if (vehicle) {
      this.form.odometer = vehicle.currentMileage;
    }
  }

  submitFuelRecord(): void {
    if (!this.isCompanyOwner()) {
      return;
    }
    const vehicle = this.selectedVehicle();
    if (!vehicle) {
      this.formError.set('Please select a vehicle for this fuel log.');
      return;
    }
    if (this.form.quantityLiters <= 0) {
      this.formError.set('Quantity in liters must be greater than zero.');
      return;
    }
    if (this.form.unitPrice < 0) {
      this.formError.set('Unit price cannot be negative.');
      return;
    }
    if (this.form.odometer < vehicle.currentMileage) {
      this.formError.set(
        `Odometer (${this.form.odometer} km) cannot be lower than the vehicle's current mileage (${vehicle.currentMileage} km).`,
      );
      return;
    }

    this.saving.set(true);
    this.formError.set(null);
    this.assignmentService.getActive(vehicle.id).subscribe({
      next: (assignment) => this.executeCreate(vehicle.id, assignment?.driverId ?? null),
      error: () => this.executeCreate(vehicle.id, null),
    });
  }

  deleteRecord(record: FuelRecord): void {
    if (!this.isCompanyOwner() || this.loading() || this.deletingId()) {
      return;
    }
    this.deletingId.set(record.id);
    this.fuelService
      .delete(record.id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => {
          this.noticeMessage.set('Fuel record removed.');
          this.loadFuelRecords();
        },
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(getApiMessage(error, 'Failed to delete fuel record.')),
      });
  }

  formatMoney(value: number, currencyCode = 'ETB'): string {
    return new Intl.NumberFormat('en-ET', {
      style: 'currency',
      currency: currencyCode,
      maximumFractionDigits: 2,
    }).format(value);
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat('en-GB', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  private executeCreate(vehicleId: string, driverId: string | null): void {
    const request: CreateFuelRecordRequest = {
      vehicleId,
      driverId,
      tripId: null,
      occurredAt: new Date(this.form.occurredAt).toISOString(),
      quantityLiters: Number(this.form.quantityLiters),
      unitPrice: Number(this.form.unitPrice),
      odometer: Number(this.form.odometer),
      station: this.form.station.trim() || null,
      currencyCode: 'ETB',
      receiptReference: this.form.receiptReference.trim() || null,
      notes: this.form.notes.trim() || null,
    };

    this.fuelService
      .create(request)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (record) => {
          this.formOpen.set(false);
          this.noticeMessage.set(
            `Logged ${record.quantityLiters} L for ${record.vehiclePlateNumber} (${this.formatMoney(record.totalCost, record.currencyCode)}).`,
          );
          this.loadFuelRecords();
        },
        error: (error: HttpErrorResponse) =>
          this.formError.set(getApiMessage(error, 'Failed to log fuel record.')),
      });
  }
}

function localDateTimeValue(): string {
  const now = new Date();
  const offset = now.getTimezoneOffset();
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 16);
}
