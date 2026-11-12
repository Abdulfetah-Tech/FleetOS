import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { getApiMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { AssignVehicleRequest, VehicleAssignment } from '../../core/assignments/vehicle-assignment.models';
import { VehicleAssignmentService } from '../../core/assignments/vehicle-assignment.service';
import { Driver, DriverPage, DriverStatus } from '../../core/drivers/driver.models';
import { DriverService } from '../../core/drivers/driver.service';
import { VehicleService } from '../../core/vehicles/vehicle.service';
import {
  CreateVehicleRequest,
  FUEL_TYPES,
  FuelType,
  UpdateVehicleRequest,
  Vehicle,
  VehiclePage,
  VehicleStatus,
  VEHICLE_STATUSES,
  VEHICLE_TYPES,
  VehicleType,
} from '../../core/vehicles/vehicle.models';

interface VehicleForm {
  plateNumber: string;
  make: string;
  model: string;
  year: number;
  type: VehicleType;
  fuelType: FuelType;
  currentMileage: number;
  vin: string;
  currencyCode: string;
  purchaseDate: string;
}

interface AssignmentState {
  loaded: boolean;
  loading: boolean;
  assignment: VehicleAssignment | null;
  error: string | null;
}

@Component({
  selector: 'app-vehicle-management',
  standalone: true,
  imports: [FormsModule],
  host: { class: 'vehicle-management' },
  templateUrl: './vehicle-management.component.html',
})
export class VehicleManagementComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly assignmentService = inject(VehicleAssignmentService);
  private readonly driverService = inject(DriverService);
  private readonly auth = inject(AuthService);

  protected readonly vehiclePage = signal<VehiclePage | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly noticeMessage = signal<string | null>(null);
  protected readonly formOpen = signal(false);
  protected readonly editingVehicleId = signal<string | null>(null);
  protected readonly busyVehicleId = signal<string | null>(null);
  protected readonly statusDrafts = signal<Record<string, VehicleStatus>>({});
  protected readonly assignmentStates = signal<Record<string, AssignmentState>>({});
  protected readonly assignmentPanelVehicleId = signal<string | null>(null);
  protected readonly assigningVehicleId = signal<string | null>(null);
  protected readonly assignmentBusyVehicleId = signal<string | null>(null);
  protected readonly assignmentSaving = signal(false);
  protected readonly driverCandidatePage = signal<DriverPage | null>(null);
  protected readonly candidateSearchText = signal('');
  protected readonly candidateSearchError = signal<string | null>(null);
  protected readonly candidatesLoading = signal(false);
  protected readonly selectedDriverId = signal('');
  protected readonly assignmentNotes = signal('');
  protected readonly searchText = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = 20;
  protected readonly candidatePageSize = 20;
  protected readonly maximumVehicleYear = new Date().getFullYear() + 1;
  protected readonly isCompanyOwner = computed(
    () => this.auth.currentSession()?.user.roles.includes('CompanyOwner') ?? false,
  );
  protected readonly vehicles = computed(() => this.vehiclePage()?.items ?? []);
  protected readonly eligibleDrivers = computed(
    () => this.driverCandidatePage()?.items.filter(
      (driver) => driver.status === DriverStatus.Active && !driver.licenseExpired,
    ) ?? [],
  );
  protected readonly candidateTotalPages = computed(
    () => Math.max(1, Math.ceil((this.driverCandidatePage()?.totalCount ?? 0) / this.candidatePageSize)),
  );
  protected readonly totalPages = computed(
    () => Math.max(1, Math.ceil((this.vehiclePage()?.totalCount ?? 0) / this.pageSize)),
  );

  protected form: VehicleForm = emptyVehicleForm();
  protected readonly vehicleTypes = VEHICLE_TYPES;
  protected readonly fuelTypes = FUEL_TYPES;
  protected readonly vehicleStatuses = VEHICLE_STATUSES;
  protected readonly activeVehicleStatus = VehicleStatus.Active;

  ngOnInit(): void {
    this.loadVehicles();
  }

  protected loadVehicles(page = this.page()): void {
    if (this.loading()) {
      return;
    }
    this.loading.set(true);
    this.errorMessage.set(null);
    this.vehicleService.list(page, this.pageSize, this.searchText()).pipe(
      finalize(() => this.loading.set(false)),
    ).subscribe({
      next: (result) => {
        this.vehiclePage.set(result);
        this.page.set(result.page);
        this.statusDrafts.set({});
      },
      error: (error: unknown) => this.errorMessage.set(getVehicleErrorMessage(error, 'Vehicle records could not be loaded.')),
    });
  }

  protected searchVehicles(): void {
    if (this.loading()) {
      return;
    }
    this.page.set(1);
    this.loadVehicles(1);
  }

  protected goToPage(page: number): void {
    if (this.loading() || page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.loadVehicles(page);
  }

  protected beginCreate(): void {
    this.editingVehicleId.set(null);
    this.form = emptyVehicleForm();
    this.formError.set(null);
    this.noticeMessage.set(null);
    this.formOpen.set(true);
  }

  protected beginEdit(vehicle: Vehicle): void {
    this.editingVehicleId.set(vehicle.id);
    this.form = {
      plateNumber: vehicle.plateNumber,
      make: vehicle.make,
      model: vehicle.model,
      year: vehicle.year,
      type: vehicle.type,
      fuelType: vehicle.fuelType,
      currentMileage: vehicle.currentMileage,
      vin: vehicle.vin ?? '',
      currencyCode: vehicle.currencyCode,
      purchaseDate: vehicle.purchaseDate ?? '',
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

  protected saveVehicle(): void {
    if (this.loading() || this.saving()) {
      return;
    }

    this.formError.set(null);
    const vehicleId = this.editingVehicleId();
    const request = {
      plateNumber: this.form.plateNumber.trim(),
      make: this.form.make.trim(),
      model: this.form.model.trim(),
      year: Number(this.form.year),
      type: this.form.type,
      fuelType: this.form.fuelType,
      vin: this.form.vin.trim() || null,
      purchaseDate: this.form.purchaseDate || null,
    };
    this.saving.set(true);
    const saveRequest = vehicleId
      ? this.vehicleService.update(vehicleId, request satisfies UpdateVehicleRequest)
      : this.vehicleService.create({
          ...request,
          currentMileage: Number(this.form.currentMileage),
          currencyCode: this.form.currencyCode.trim().toUpperCase(),
        } satisfies CreateVehicleRequest);

    saveRequest.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.formOpen.set(false);
        this.noticeMessage.set(vehicleId ? 'Vehicle details updated.' : 'Vehicle added to the fleet.');
        this.page.set(vehicleId ? this.page() : 1);
        this.loadVehicles(vehicleId ? this.page() : 1);
      },
      error: (error: unknown) => this.formError.set(getVehicleErrorMessage(error, 'Vehicle changes could not be saved.')),
    });
  }

  protected statusDraft(vehicle: Vehicle): VehicleStatus {
    return this.statusDrafts()[vehicle.id] ?? vehicle.status;
  }

  protected setStatusDraft(vehicle: Vehicle, value: string): void {
    const status = parseVehicleStatus(value);
    if (!status) {
      this.errorMessage.set('The selected vehicle status is invalid.');
      return;
    }
    this.statusDrafts.update((drafts) => ({ ...drafts, [vehicle.id]: status }));
  }

  protected updateStatus(vehicle: Vehicle): void {
    const status = this.statusDraft(vehicle);
    if (this.loading() || status === vehicle.status || this.busyVehicleId()) {
      return;
    }

    this.errorMessage.set(null);
    this.noticeMessage.set(null);
    this.busyVehicleId.set(vehicle.id);
    this.vehicleService.changeStatus(vehicle.id, status).pipe(
      finalize(() => this.busyVehicleId.set(null)),
    ).subscribe({
      next: () => {
        this.statusDrafts.update((drafts) => ({ ...drafts, [vehicle.id]: status }));
        this.noticeMessage.set(`Status updated for ${vehicle.plateNumber}.`);
        this.loadVehicles();
      },
      error: (error: unknown) => this.errorMessage.set(getVehicleErrorMessage(error, 'Vehicle status could not be changed.')),
    });
  }

  protected assignmentState(vehicleId: string): AssignmentState {
    return this.assignmentStates()[vehicleId] ?? emptyAssignmentState();
  }

  protected toggleAssignmentPanel(vehicle: Vehicle): void {
    if (this.assignmentPanelVehicleId() === vehicle.id) {
      this.assignmentPanelVehicleId.set(null);
      this.closeAssignmentForm();
      return;
    }

    this.assignmentPanelVehicleId.set(vehicle.id);
    this.closeAssignmentForm();
    this.loadAssignment(vehicle);
  }

  protected loadAssignment(vehicle: Vehicle): void {
    if (this.assignmentState(vehicle.id).loading) {
      return;
    }

    this.updateAssignmentState(vehicle.id, { loading: true, error: null });
    this.assignmentService.getActive(vehicle.id).pipe(
      finalize(() => this.updateAssignmentState(vehicle.id, { loading: false })),
    ).subscribe({
      next: (assignment) => this.updateAssignmentState(vehicle.id, {
        loaded: true,
        assignment,
        error: null,
      }),
      error: (error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 404) {
          this.updateAssignmentState(vehicle.id, { loaded: true, assignment: null, error: null });
          return;
        }
        this.updateAssignmentState(vehicle.id, {
          error: getVehicleErrorMessage(error, 'The vehicle assignment could not be loaded.'),
        });
      },
    });
  }

  protected beginAssignment(vehicle: Vehicle): void {
    const state = this.assignmentState(vehicle.id);
    if (!this.isCompanyOwner() || vehicle.status !== VehicleStatus.Active || !state.loaded ||
      state.assignment || this.assignmentSaving()) {
      return;
    }

    this.assigningVehicleId.set(vehicle.id);
    this.driverCandidatePage.set(null);
    this.candidateSearchText.set('');
    this.candidateSearchError.set(null);
    this.selectedDriverId.set('');
    this.assignmentNotes.set('');
  }

  protected closeAssignmentForm(): void {
    if (this.assignmentSaving()) {
      return;
    }
    this.assigningVehicleId.set(null);
    this.driverCandidatePage.set(null);
    this.candidateSearchText.set('');
    this.candidateSearchError.set(null);
    this.selectedDriverId.set('');
    this.assignmentNotes.set('');
  }

  protected searchDriverCandidates(page = 1): void {
    if (this.candidatesLoading() || this.assignmentSaving()) {
      return;
    }

    const search = this.candidateSearchText().trim();
    if (!search) {
      this.driverCandidatePage.set(null);
      this.selectedDriverId.set('');
      this.candidateSearchError.set('Enter a driver name, email, phone number, or license to search.');
      return;
    }

    this.candidateSearchError.set(null);
    this.driverCandidatePage.set(null);
    this.selectedDriverId.set('');
    this.candidatesLoading.set(true);
    this.driverService.list(page, this.candidatePageSize, search).pipe(
      finalize(() => this.candidatesLoading.set(false)),
    ).subscribe({
      next: (result) => this.driverCandidatePage.set(result),
      error: (error: unknown) => this.candidateSearchError.set(
        getVehicleErrorMessage(error, 'Eligible drivers could not be loaded.'),
      ),
    });
  }

  protected goToCandidatePage(page: number): void {
    if (this.candidatesLoading() || page < 1 || page > this.candidateTotalPages() ||
      page === this.driverCandidatePage()?.page) {
      return;
    }
    this.searchDriverCandidates(page);
  }

  protected assignDriver(vehicle: Vehicle): void {
    const driver = this.eligibleDrivers().find((item) => item.id === this.selectedDriverId());
    if (!this.isCompanyOwner() || vehicle.status !== VehicleStatus.Active || !driver ||
      this.loading() || this.assignmentSaving() || this.assignmentBusyVehicleId()) {
      return;
    }

    this.updateAssignmentState(vehicle.id, { error: null });
    this.assignmentSaving.set(true);
    this.assignmentBusyVehicleId.set(vehicle.id);
    const request: AssignVehicleRequest = {
      vehicleId: vehicle.id,
      driverId: driver.id,
      notes: nullableTrimmed(this.assignmentNotes()),
    };
    this.assignmentService.assign(vehicle.id, request).pipe(
      finalize(() => {
        this.assignmentSaving.set(false);
        this.assignmentBusyVehicleId.set(null);
      }),
    ).subscribe({
      next: (assignment) => {
        this.updateAssignmentState(vehicle.id, { loaded: true, assignment, error: null });
        this.assigningVehicleId.set(null);
        this.noticeMessage.set(`${assignment.driverFullName} assigned to ${vehicle.plateNumber}.`);
      },
      error: (error: unknown) => this.updateAssignmentState(vehicle.id, {
        error: getVehicleErrorMessage(error, 'The driver could not be assigned to this vehicle.'),
      }),
    });
  }

  protected endAssignment(vehicle: Vehicle, assignment: VehicleAssignment): void {
    if (!this.isCompanyOwner() || this.loading() || this.assignmentSaving() ||
      this.assignmentBusyVehicleId() ||
      !window.confirm(`End ${assignment.driverFullName}'s assignment to ${vehicle.plateNumber}?`)) {
      return;
    }

    this.updateAssignmentState(vehicle.id, { error: null });
    this.assignmentBusyVehicleId.set(vehicle.id);
    this.assignmentService.unassign(vehicle.id).pipe(
      finalize(() => this.assignmentBusyVehicleId.set(null)),
    ).subscribe({
      next: () => {
        this.updateAssignmentState(vehicle.id, { loaded: true, assignment: null, error: null });
        this.noticeMessage.set(`Assignment ended for ${vehicle.plateNumber}.`);
      },
      error: (error: unknown) => this.updateAssignmentState(vehicle.id, {
        error: getVehicleErrorMessage(error, 'The vehicle assignment could not be ended.'),
      }),
    });
  }

  protected formatAssignmentTime(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? value
      : new Intl.DateTimeFormat('en-ET', { dateStyle: 'medium', timeStyle: 'short' }).format(date);
  }

  protected deleteVehicle(vehicle: Vehicle): void {
    if (!this.isCompanyOwner() || this.loading() || this.busyVehicleId() ||
      !window.confirm(`Remove ${vehicle.plateNumber} from this fleet?`)) {
      return;
    }

    this.errorMessage.set(null);
    this.noticeMessage.set(null);
    this.busyVehicleId.set(vehicle.id);
    this.vehicleService.delete(vehicle.id).pipe(
      finalize(() => this.busyVehicleId.set(null)),
    ).subscribe({
      next: () => {
        this.noticeMessage.set(`${vehicle.plateNumber} was removed from the fleet.`);
        const remainingCount = (this.vehiclePage()?.totalCount ?? 1) - 1;
        const lastPage = Math.max(1, Math.ceil(remainingCount / this.pageSize));
        this.page.set(Math.min(this.page(), lastPage));
        this.loadVehicles(this.page());
      },
      error: (error: unknown) => this.errorMessage.set(getVehicleErrorMessage(error, 'Vehicle could not be removed.')),
    });
  }

  protected statusLabel(status: VehicleStatus): string {
    return VEHICLE_STATUSES.find((item) => item.value === status)?.label ?? 'Unknown';
  }

  protected typeLabel(type: VehicleType): string {
    return VEHICLE_TYPES.find((item) => item.value === type)?.label ?? 'Unknown';
  }

  protected fuelLabel(fuelType: FuelType): string {
    return FUEL_TYPES.find((item) => item.value === fuelType)?.label ?? 'Unknown';
  }

  protected formatMileage(value: number): string {
    return new Intl.NumberFormat('en-ET', { maximumFractionDigits: 1 }).format(value);
  }

  private updateAssignmentState(vehicleId: string, update: Partial<AssignmentState>): void {
    this.assignmentStates.update((states) => ({
      ...states,
      [vehicleId]: {
        ...emptyAssignmentState(),
        ...states[vehicleId],
        ...update,
      },
    }));
  }
}

function emptyVehicleForm(): VehicleForm {
  return {
    plateNumber: '',
    make: '',
    model: '',
    year: new Date().getFullYear(),
    type: VehicleType.Pickup,
    fuelType: FuelType.Diesel,
    currentMileage: 0,
    vin: '',
    currencyCode: 'ETB',
    purchaseDate: '',
  };
}

function parseVehicleStatus(value: string): VehicleStatus | undefined {
  const parsed = Number(value);
  return VEHICLE_STATUSES.find((item) => item.value === parsed)?.value;
}

function emptyAssignmentState(): AssignmentState {
  return { loaded: false, loading: false, assignment: null, error: null };
}

function nullableTrimmed(value: string): string | null {
  const trimmed = value.trim();
  return trimmed || null;
}

function getVehicleErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse && error.status === 0) {
    return 'The fleet API could not be reached. Check the server connection, then try again.';
  }
  if (error instanceof HttpErrorResponse && error.status === 401) {
    return 'Your session could not be renewed. Sign in again to continue.';
  }
  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'Your account does not have permission to complete this vehicle operation.';
  }
  return getApiMessage(error) ?? fallback;
}
