import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, finalize } from 'rxjs';
import { RouterLink } from '@angular/router';
import { getApiMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { VehicleAssignment } from '../../core/assignments/vehicle-assignment.models';
import { VehicleAssignmentService } from '../../core/assignments/vehicle-assignment.service';
import { Vehicle, VehiclePage, VehicleStatus } from '../../core/vehicles/vehicle.models';
import { VehicleService } from '../../core/vehicles/vehicle.service';
import {
  CompleteTripRequest,
  CreateTripRequest,
  Trip,
  TripPage,
  TripStatus,
  TRIP_STATUSES,
} from '../../core/trips/trip.models';
import { TripService } from '../../core/trips/trip.service';

interface TripForm {
  origin: string;
  destination: string;
  purpose: string;
  plannedStart: string;
  startMileage: number;
  notes: string;
}

@Component({
  selector: 'app-trip-management',
  standalone: true,
  imports: [FormsModule, RouterLink],
  host: { class: 'trip-management' },
  templateUrl: './trip-management.component.html',
})
export class TripManagementComponent implements OnInit {
  private readonly tripService = inject(TripService);
  private readonly vehicleService = inject(VehicleService);
  private readonly assignmentService = inject(VehicleAssignmentService);
  private readonly auth = inject(AuthService);

  protected readonly tripPage = signal<TripPage | null>(null);
  protected readonly vehiclePage = signal<VehiclePage | null>(null);
  protected readonly loading = signal(false);
  protected readonly vehicleChoicesLoading = signal(false);
  protected readonly assignmentLoading = signal(false);
  protected readonly saving = signal(false);
  protected readonly busyTripId = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly vehicleSearchError = signal<string | null>(null);
  protected readonly assignmentError = signal<string | null>(null);
  protected readonly noticeMessage = signal<string | null>(null);
  protected readonly formOpen = signal(false);
  protected readonly completeTripId = signal<string | null>(null);
  protected readonly activeAssignment = signal<VehicleAssignment | null>(null);
  protected readonly assignmentLoaded = signal(false);
  protected readonly vehicleSearchText = signal('');
  protected readonly selectedVehicleId = signal('');
  protected readonly statusFilter = signal<TripStatus | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = 20;
  protected readonly statuses = TRIP_STATUSES;
  protected readonly activeVehicleStatus = VehicleStatus.Active;
  protected readonly plannedTripStatus = TripStatus.Planned;
  protected readonly inProgressTripStatus = TripStatus.InProgress;
  protected readonly completedTripStatus = TripStatus.Completed;
  protected readonly cancelledTripStatus = TripStatus.Cancelled;
  protected readonly isCompanyOwner = computed(
    () => this.auth.currentSession()?.user.roles.includes('CompanyOwner') ?? false,
  );
  protected readonly trips = computed(() => this.tripPage()?.items ?? []);
  protected readonly activeVehicles = computed(
    () =>
      this.vehiclePage()?.items.filter((vehicle) => vehicle.status === VehicleStatus.Active) ?? [],
  );
  protected readonly selectedVehicle = computed(
    () =>
      this.vehiclePage()?.items.find((vehicle) => vehicle.id === this.selectedVehicleId()) ?? null,
  );
  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil((this.tripPage()?.totalCount ?? 0) / this.pageSize)),
  );
  protected readonly totalVehiclePages = computed(() =>
    Math.max(1, Math.ceil((this.vehiclePage()?.totalCount ?? 0) / this.pageSize)),
  );

  protected form: TripForm = emptyTripForm();
  protected completeActualEnd = localDateTimeInput(new Date());
  protected completeEndMileage = '';

  ngOnInit(): void {
    this.loadTrips();
  }

  protected loadTrips(page = this.page()): void {
    if (this.loading()) {
      return;
    }
    this.loading.set(true);
    this.errorMessage.set(null);
    this.tripService
      .list(page, this.pageSize, this.statusFilter())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          this.tripPage.set(result);
          this.page.set(result.page);
        },
        error: (error: unknown) =>
          this.errorMessage.set(getTripErrorMessage(error, 'Trip records could not be loaded.')),
      });
  }

  protected setStatusFilter(value: string): void {
    if (value === '') {
      this.statusFilter.set(null);
    } else {
      const status = parseTripStatus(value);
      if (status === undefined) {
        this.errorMessage.set('The selected trip status is invalid.');
        return;
      }
      this.statusFilter.set(status);
    }
    this.page.set(1);
    this.loadTrips(1);
  }

  protected goToPage(page: number): void {
    if (this.loading() || page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.loadTrips(page);
  }

  protected beginCreate(): void {
    if (!this.isCompanyOwner() || this.loading() || this.saving()) {
      return;
    }
    this.form = emptyTripForm();
    this.formError.set(null);
    this.vehicleSearchError.set(null);
    this.assignmentError.set(null);
    this.vehicleSearchText.set('');
    this.selectedVehicleId.set('');
    this.vehiclePage.set(null);
    this.activeAssignment.set(null);
    this.assignmentLoaded.set(false);
    this.noticeMessage.set(null);
    this.formOpen.set(true);
    this.loadVehicleChoices(1);
  }

  protected closeForm(): void {
    if (this.saving()) {
      return;
    }
    this.formOpen.set(false);
    this.formError.set(null);
  }

  protected searchVehicles(page = 1): void {
    this.loadVehicleChoices(page);
  }

  protected selectVehicle(vehicleId: string): void {
    if (this.assignmentLoading()) {
      return;
    }
    this.selectedVehicleId.set(vehicleId);
    this.activeAssignment.set(null);
    this.assignmentLoaded.set(false);
    this.assignmentError.set(null);
    const vehicle = this.vehiclePage()?.items.find((item) => item.id === vehicleId);
    if (vehicle) {
      this.form.startMileage = vehicle.currentMileage;
      this.loadActiveAssignment(vehicle);
    }
  }

  protected createTrip(): void {
    const vehicle = this.selectedVehicle();
    const assignment = this.activeAssignment();
    const plannedStart = new Date(this.form.plannedStart);
    if (
      !this.isCompanyOwner() ||
      !vehicle ||
      vehicle.status !== VehicleStatus.Active ||
      !assignment ||
      !this.assignmentLoaded() ||
      this.saving()
    ) {
      return;
    }
    this.formError.set(null);
    if (!Number.isFinite(plannedStart.getTime())) {
      this.formError.set('Enter a valid planned departure time.');
      return;
    }
    const startMileage = Number(this.form.startMileage);
    if (!isValidMileage(startMileage) || startMileage < vehicle.currentMileage) {
      this.formError.set(
        'Starting odometer must be at least the vehicle’s current reading and use at most one decimal place.',
      );
      return;
    }

    this.saving.set(true);
    const request: CreateTripRequest = {
      vehicleId: vehicle.id,
      driverId: assignment.driverId,
      origin: this.form.origin.trim(),
      destination: this.form.destination.trim(),
      purpose: this.form.purpose.trim(),
      plannedStart: plannedStart.toISOString(),
      startMileage,
      notes: nullableTrimmed(this.form.notes),
    };
    this.tripService
      .create(request)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (trip) => {
          this.formOpen.set(false);
          this.noticeMessage.set(`Trip from ${trip.origin} to ${trip.destination} was scheduled.`);
          this.page.set(1);
          this.loadTrips(1);
        },
        error: (error: unknown) =>
          this.formError.set(getTripErrorMessage(error, 'The trip could not be scheduled.')),
      });
  }

  protected startTrip(trip: Trip): void {
    if (
      trip.status !== TripStatus.Planned ||
      this.isTripStartInFuture(trip) ||
      this.loading() ||
      this.busyTripId()
    ) {
      return;
    }
    this.runTripAction(trip, 'Trip started.', () => this.tripService.start(trip.id));
  }

  protected beginComplete(trip: Trip): void {
    if (trip.status !== TripStatus.InProgress || this.loading() || this.busyTripId()) {
      return;
    }
    this.completeTripId.set(trip.id);
    this.completeActualEnd = localDateTimeInput(new Date());
    this.completeEndMileage = String(trip.startMileage);
    this.formError.set(null);
  }

  protected closeComplete(): void {
    if (this.saving()) {
      return;
    }
    this.completeTripId.set(null);
    this.formError.set(null);
  }

  protected completeTrip(trip: Trip): void {
    const actualEnd = new Date(this.completeActualEnd);
    const endMileage = Number(this.completeEndMileage);
    if (
      trip.status !== TripStatus.InProgress ||
      !Number.isFinite(actualEnd.getTime()) ||
      !isValidMileage(endMileage) ||
      endMileage < trip.startMileage ||
      (trip.actualStart !== null && actualEnd.getTime() < new Date(trip.actualStart).getTime()) ||
      this.loading() ||
      this.busyTripId()
    ) {
      this.formError.set(
        'End time cannot precede trip start. Enter an odometer reading at least as high as the trip start, with at most one decimal place.',
      );
      return;
    }

    this.formError.set(null);
    this.busyTripId.set(trip.id);
    this.tripService
      .complete(trip.id, {
        actualEnd: actualEnd.toISOString(),
        endMileage,
      } satisfies CompleteTripRequest)
      .pipe(finalize(() => this.busyTripId.set(null)))
      .subscribe({
        next: () => {
          this.completeTripId.set(null);
          this.noticeMessage.set(`Trip from ${trip.origin} to ${trip.destination} was completed.`);
          this.loadTrips();
        },
        error: (error: unknown) =>
          this.formError.set(getTripErrorMessage(error, 'The trip could not be completed.')),
      });
  }

  protected cancelTrip(trip: Trip): void {
    if (
      !this.isCompanyOwner() ||
      trip.status === TripStatus.Completed ||
      trip.status === TripStatus.Cancelled ||
      this.loading() ||
      this.busyTripId() ||
      !window.confirm(`Cancel the trip from ${trip.origin} to ${trip.destination}?`)
    ) {
      return;
    }
    this.runTripAction(trip, 'Trip cancelled.', () => this.tripService.cancel(trip.id));
  }

  protected canStart(trip: Trip): boolean {
    return trip.status === TripStatus.Planned && !this.isTripStartInFuture(trip);
  }

  protected isTripStartInFuture(trip: Trip): boolean {
    return new Date(trip.plannedStart).getTime() > Date.now();
  }

  protected statusLabel(status: TripStatus): string {
    return TRIP_STATUSES.find((item) => item.value === status)?.label ?? 'Unknown';
  }

  protected formatDate(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? value
      : new Intl.DateTimeFormat('en-ET', { dateStyle: 'medium', timeStyle: 'short' }).format(date);
  }

  protected toLocalDateTime(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '' : localDateTimeInput(date);
  }

  protected formatMileage(value: number | null): string {
    return value === null
      ? '—'
      : `${new Intl.NumberFormat('en-ET', { maximumFractionDigits: 1 }).format(value)} km`;
  }

  protected candidatePageLabel(): string {
    const currentPage = this.vehiclePage();
    return currentPage
      ? `Page ${currentPage.page} of ${Math.max(1, Math.ceil(currentPage.totalCount / currentPage.pageSize))}`
      : '';
  }

  protected goToVehiclePage(page: number): void {
    const current = this.vehiclePage();
    const lastPage = current ? Math.max(1, Math.ceil(current.totalCount / current.pageSize)) : 1;
    if (!this.vehicleChoicesLoading() && page >= 1 && page <= lastPage && page !== current?.page) {
      this.loadVehicleChoices(page);
    }
  }

  private loadVehicleChoices(page: number): void {
    if (this.vehicleChoicesLoading()) {
      return;
    }
    this.vehicleChoicesLoading.set(true);
    this.vehicleSearchError.set(null);
    this.vehiclePage.set(null);
    this.vehicleService
      .list(page, this.pageSize, this.vehicleSearchText())
      .pipe(finalize(() => this.vehicleChoicesLoading.set(false)))
      .subscribe({
        next: (result) => {
          this.vehiclePage.set(result);
          if (
            this.selectedVehicleId() &&
            !result.items.some((vehicle) => vehicle.id === this.selectedVehicleId())
          ) {
            this.selectedVehicleId.set('');
            this.activeAssignment.set(null);
            this.assignmentLoaded.set(false);
          }
        },
        error: (error: unknown) =>
          this.vehicleSearchError.set(
            getTripErrorMessage(
              error,
              'Vehicles could not be loaded. Check the connection and try again.',
            ),
          ),
      });
  }

  private loadActiveAssignment(vehicle: Vehicle): void {
    this.assignmentLoading.set(true);
    this.assignmentService
      .getActive(vehicle.id)
      .pipe(finalize(() => this.assignmentLoading.set(false)))
      .subscribe({
        next: (assignment) => {
          this.activeAssignment.set(assignment);
          this.assignmentLoaded.set(true);
        },
        error: (error: unknown) => {
          if (error instanceof HttpErrorResponse && error.status === 404) {
            this.activeAssignment.set(null);
            this.assignmentLoaded.set(true);
            return;
          }
          this.assignmentError.set(
            getTripErrorMessage(error, 'The vehicle assignment could not be checked.'),
          );
          this.assignmentLoaded.set(false);
        },
      });
  }

  private runTripAction(trip: Trip, successMessage: string, action: () => Observable<Trip>): void {
    this.errorMessage.set(null);
    this.busyTripId.set(trip.id);
    action()
      .pipe(finalize(() => this.busyTripId.set(null)))
      .subscribe({
        next: () => {
          this.noticeMessage.set(`${successMessage} ${trip.origin} to ${trip.destination}`);
          this.loadTrips();
        },
        error: (error: unknown) =>
          this.errorMessage.set(getTripErrorMessage(error, 'The trip could not be updated.')),
      });
  }
}

function emptyTripForm(): TripForm {
  return {
    origin: '',
    destination: '',
    purpose: '',
    plannedStart: localDateTimeInput(new Date()),
    startMileage: 0,
    notes: '',
  };
}

function localDateTimeInput(value: Date): string {
  const offset = value.getTimezoneOffset() * 60_000;
  return new Date(value.getTime() - offset).toISOString().slice(0, 16);
}

function parseTripStatus(value: string): TripStatus | undefined {
  const parsed = Number(value);
  return TRIP_STATUSES.find((item) => item.value === parsed)?.value;
}

function nullableTrimmed(value: string): string | null {
  const trimmed = value.trim();
  return trimmed || null;
}

function isValidMileage(value: number): boolean {
  return Number.isFinite(value) && value >= 0 && Math.round(value * 10) / 10 === value;
}

function getTripErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse && error.status === 0) {
    return 'The fleet API could not be reached. Check the server connection, then try again.';
  }
  if (error instanceof HttpErrorResponse && error.status === 401) {
    return 'Your session could not be renewed. Sign in again to continue.';
  }
  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'Your account does not have permission to complete this trip operation.';
  }
  return getApiMessage(error) ?? fallback;
}
