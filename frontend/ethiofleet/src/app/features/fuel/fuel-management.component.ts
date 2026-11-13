import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { RouterLink } from '@angular/router';
import { getApiMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { VehicleAssignmentService } from '../../core/assignments/vehicle-assignment.service';
import { Vehicle, VehiclePage, VehicleStatus } from '../../core/vehicles/vehicle.models';
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
  imports: [FormsModule, RouterLink, DatePipe, DecimalPipe],
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
  protected readonly filterVehicleId = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);

  protected readonly form: FuelForm = {
    occurredAt: new Date().toISOString().slice(0, 16),
    quantityLiters: 50,
    unitPrice: 85,
    odometer: 0,
    station: '',
    receiptReference: '',
    notes: '',
  };

  protected readonly isCompanyOwner = computed(() => this.auth.isCompanyOwner());
  protected readonly isFleetManager = computed(() => this.auth.isFleetManager());

  protected readonly computedTotalCost = computed(() => {
    const qty = Number(this.form.quantityLiters) || 0;
    const price = Number(this.form.unitPrice) || 0;
    return Math.round(qty * price * 100) / 100;
  });

  protected readonly totalCostCurrentPage = computed(() => {
    return (
      this.fuelPage()?.items.reduce((sum, item) => sum + (Number(item.totalCost) || 0), 0) ?? 0
    );
  });

  protected readonly totalLitersCurrentPage = computed(() => {
    return (
      this.fuelPage()?.items.reduce((sum, item) => sum + (Number(item.quantityLiters) || 0), 0) ?? 0
    );
  });

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
      .list(this.page(), this.pageSize(), this.filterVehicleId(), null)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (page) => this.fuelPage.set(page),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(getApiMessage(error, 'Failed to load fuel records.')),
      });
  }

  setVehicleFilter(vehicleId: string | null): void {
    this.filterVehicleId.set(vehicleId);
    this.page.set(1);
    this.loadFuelRecords();
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((p) => p + 1);
      this.loadFuelRecords();
    }
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.loadFuelRecords();
    }
  }

  beginCreate(): void {
    this.formOpen.set(true);
    this.formError.set(null);
    this.noticeMessage.set(null);
    this.selectedVehicle.set(null);
    this.form.occurredAt = new Date().toISOString().slice(0, 16);
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
      .list({ page: 1, pageSize: 25, search: this.vehicleSearchText().trim() || null })
      .pipe(finalize(() => this.vehicleChoicesLoading.set(false)))
      .subscribe({
        next: (page) => this.vehiclePage.set(page),
        error: (error: HttpErrorResponse) =>
          this.formError.set(getApiMessage(error, 'Could not load vehicle candidates.')),
      });
  }

  chooseVehicle(vehicle: Vehicle): void {
    this.selectedVehicle.set(vehicle);
    this.form.odometer = vehicle.currentMileage;
  }

  submitFuelRecord(): void {
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

    // Check if vehicle has an active driver assignment to link driverId automatically
    this.assignmentService.getActive(vehicle.id).subscribe({
      next: (assignment) => {
        this.executeCreate(vehicle.id, assignment?.driverId ?? null);
      },
      error: () => {
        this.executeCreate(vehicle.id, null);
      },
    });
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
            `Fuel entry logged successfully for ${record.vehiclePlateNumber}: ${record.quantityLiters} L (${record.totalCost} ETB).`,
          );
          this.loadFuelRecords();
        },
        error: (error: HttpErrorResponse) =>
          this.formError.set(getApiMessage(error, 'Failed to log fuel record.')),
      });
  }

  deleteRecord(record: FuelRecord): void {
    if (
      !confirm(
        `Are you sure you want to delete this fuel record of ${record.quantityLiters} L for ${record.vehiclePlateNumber}?`,
      )
    ) {
      return;
    }

    this.deletingId.set(record.id);
    this.fuelService
      .delete(record.id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => {
          this.noticeMessage.set('Fuel record removed successfully.');
          this.loadFuelRecords();
        },
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(getApiMessage(error, 'Failed to delete fuel record.')),
      });
  }
}
