import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { getApiMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import {
  CompleteMaintenanceScheduleRequest,
  CreateMaintenanceRecordRequest,
  CreateMaintenanceScheduleRequest,
  MAINTENANCE_TYPES,
  MaintenanceRecordPage,
  MaintenanceSchedule,
  MaintenanceSchedulePage,
  MaintenanceScheduleStatus,
  MaintenanceType,
} from '../../core/maintenance/maintenance.models';
import { MaintenanceService } from '../../core/maintenance/maintenance.service';
import { VehiclePage } from '../../core/vehicles/vehicle.models';
import { VehicleService } from '../../core/vehicles/vehicle.service';

interface MaintenanceRecordForm {
  vehicleId: string;
  type: MaintenanceType;
  description: string;
  performedOn: string;
  odometer: number;
  laborCost: number;
  partsCost: number;
  serviceProvider: string;
  reference: string;
  notes: string;
}

interface MaintenanceScheduleForm {
  vehicleId: string;
  name: string;
  description: string;
  nextDueDate: string;
  nextDueMileage: number | null;
  intervalDays: number | null;
  intervalMileage: number | null;
}

@Component({
  selector: 'app-maintenance-management',
  standalone: true,
  imports: [FormsModule],
  host: { class: 'maintenance-management' },
  templateUrl: './maintenance-management.component.html',
})
export class MaintenanceManagementComponent implements OnInit {
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly vehicleService = inject(VehicleService);
  private readonly auth = inject(AuthService);

  protected readonly activeTab = signal<'records' | 'schedules'>('records');
  protected readonly recordsPage = signal<MaintenanceRecordPage | null>(null);
  protected readonly schedulesPage = signal<MaintenanceSchedulePage | null>(null);
  protected readonly vehiclePage = signal<VehiclePage | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly noticeMessage = signal<string | null>(null);
  protected readonly recordFormOpen = signal(false);
  protected readonly scheduleFormOpen = signal(false);
  protected readonly completingSchedule = signal<MaintenanceSchedule | null>(null);
  protected readonly filterVehicleId = signal('');
  protected readonly dueOnly = signal(false);
  protected readonly recordsPageNumber = signal(1);
  protected readonly schedulesPageNumber = signal(1);
  protected readonly pageSize = signal(20);

  protected readonly maintenanceTypes = MAINTENANCE_TYPES;

  protected readonly recordForm: MaintenanceRecordForm = {
    vehicleId: '',
    type: MaintenanceType.Preventive,
    description: '',
    performedOn: todayIsoDate(),
    odometer: 0,
    laborCost: 0,
    partsCost: 0,
    serviceProvider: '',
    reference: '',
    notes: '',
  };

  protected readonly scheduleForm: MaintenanceScheduleForm = {
    vehicleId: '',
    name: '',
    description: '',
    nextDueDate: '',
    nextDueMileage: null,
    intervalDays: null,
    intervalMileage: null,
  };

  protected completeForm: CompleteMaintenanceScheduleRequest = {
    completedOn: todayIsoDate(),
    completedAtMileage: 0,
  };

  protected readonly isCompanyOwner = computed(() => this.auth.isCompanyOwner());
  protected readonly records = computed(() => this.recordsPage()?.items ?? []);
  protected readonly schedules = computed(() => this.schedulesPage()?.items ?? []);
  protected readonly totalRecordPages = computed(() => {
    const p = this.recordsPage();
    return p ? Math.max(1, Math.ceil(p.totalCount / p.pageSize)) : 1;
  });
  protected readonly totalSchedulePages = computed(() => {
    const p = this.schedulesPage();
    return p ? Math.max(1, Math.ceil(p.totalCount / p.pageSize)) : 1;
  });
  protected readonly pageTotalCost = computed(() =>
    this.records().reduce((sum, r) => sum + (Number(r.totalCost) || 0), 0),
  );

  ngOnInit(): void {
    this.loadVehicles();
    this.loadRecords();
    this.loadSchedules();
  }

  switchTab(tab: 'records' | 'schedules'): void {
    this.activeTab.set(tab);
    this.errorMessage.set(null);
    this.formError.set(null);
  }

  loadVehicles(): void {
    this.vehicleService.list(1, 50, '').subscribe({
      next: (page) => this.vehiclePage.set(page),
    });
  }

  loadRecords(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.maintenanceService
      .listRecords(this.recordsPageNumber(), this.pageSize(), this.filterVehicleId() || null)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (page) => this.recordsPage.set(page),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(getApiMessage(error, 'Failed to load maintenance records.')),
      });
  }

  loadSchedules(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.maintenanceService
      .listSchedules(
        this.schedulesPageNumber(),
        this.pageSize(),
        this.filterVehicleId() || null,
        this.dueOnly(),
      )
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (page) => this.schedulesPage.set(page),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(getApiMessage(error, 'Failed to load maintenance schedules.')),
      });
  }

  onVehicleFilterChange(vehicleId: string): void {
    this.filterVehicleId.set(vehicleId);
    this.recordsPageNumber.set(1);
    this.schedulesPageNumber.set(1);
    this.loadRecords();
    this.loadSchedules();
  }

  toggleDueOnly(checked: boolean): void {
    this.dueOnly.set(checked);
    this.schedulesPageNumber.set(1);
    this.loadSchedules();
  }

  beginCreateRecord(): void {
    if (!this.isCompanyOwner()) {
      return;
    }
    this.recordFormOpen.set(true);
    this.scheduleFormOpen.set(false);
    this.formError.set(null);
    this.noticeMessage.set(null);
    this.recordForm.vehicleId = '';
    this.recordForm.type = MaintenanceType.Preventive;
    this.recordForm.description = '';
    this.recordForm.performedOn = todayIsoDate();
    this.recordForm.odometer = 0;
    this.recordForm.laborCost = 0;
    this.recordForm.partsCost = 0;
    this.recordForm.serviceProvider = '';
    this.recordForm.reference = '';
    this.recordForm.notes = '';
  }

  beginCreateSchedule(): void {
    if (!this.isCompanyOwner()) {
      return;
    }
    this.scheduleFormOpen.set(true);
    this.recordFormOpen.set(false);
    this.formError.set(null);
    this.noticeMessage.set(null);
    this.scheduleForm.vehicleId = '';
    this.scheduleForm.name = '';
    this.scheduleForm.description = '';
    this.scheduleForm.nextDueDate = '';
    this.scheduleForm.nextDueMileage = null;
    this.scheduleForm.intervalDays = null;
    this.scheduleForm.intervalMileage = null;
  }

  closeForms(): void {
    this.recordFormOpen.set(false);
    this.scheduleFormOpen.set(false);
    this.completingSchedule.set(null);
    this.formError.set(null);
  }

  submitRecord(): void {
    if (!this.recordForm.vehicleId || !this.recordForm.description.trim()) {
      this.formError.set('Vehicle and description are required.');
      return;
    }

    const payload: CreateMaintenanceRecordRequest = {
      vehicleId: this.recordForm.vehicleId,
      type: Number(this.recordForm.type) as MaintenanceType,
      description: this.recordForm.description.trim(),
      performedOn: this.recordForm.performedOn,
      odometer: Number(this.recordForm.odometer),
      laborCost: Number(this.recordForm.laborCost),
      partsCost: Number(this.recordForm.partsCost),
      currencyCode: 'ETB',
      serviceProvider: this.recordForm.serviceProvider.trim() || null,
      reference: this.recordForm.reference.trim() || null,
      notes: this.recordForm.notes.trim() || null,
    };

    this.saving.set(true);
    this.formError.set(null);
    this.maintenanceService
      .createRecord(payload)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.recordFormOpen.set(false);
          this.noticeMessage.set('Maintenance record logged.');
          this.loadRecords();
        },
        error: (error: HttpErrorResponse) =>
          this.formError.set(getApiMessage(error, 'Failed to log maintenance record.')),
      });
  }

  submitSchedule(): void {
    if (!this.scheduleForm.vehicleId || !this.scheduleForm.name.trim()) {
      this.formError.set('Vehicle and schedule name are required.');
      return;
    }

    const payload: CreateMaintenanceScheduleRequest = {
      vehicleId: this.scheduleForm.vehicleId,
      name: this.scheduleForm.name.trim(),
      description: this.scheduleForm.description.trim() || null,
      nextDueDate: this.scheduleForm.nextDueDate || null,
      nextDueMileage: this.scheduleForm.nextDueMileage !== null ? Number(this.scheduleForm.nextDueMileage) : null,
      intervalDays: this.scheduleForm.intervalDays !== null ? Number(this.scheduleForm.intervalDays) : null,
      intervalMileage: this.scheduleForm.intervalMileage !== null ? Number(this.scheduleForm.intervalMileage) : null,
    };

    this.saving.set(true);
    this.formError.set(null);
    this.maintenanceService
      .createSchedule(payload)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.scheduleFormOpen.set(false);
          this.noticeMessage.set('Maintenance schedule created.');
          this.loadSchedules();
        },
        error: (error: HttpErrorResponse) =>
          this.formError.set(getApiMessage(error, 'Failed to create maintenance schedule.')),
      });
  }

  beginCompleteSchedule(schedule: MaintenanceSchedule): void {
    if (!this.isCompanyOwner()) {
      return;
    }
    this.completingSchedule.set(schedule);
    this.completeForm = {
      completedOn: todayIsoDate(),
      completedAtMileage: schedule.nextDueMileage ?? 0,
    };
    this.formError.set(null);
    this.noticeMessage.set(null);
  }

  submitCompleteSchedule(): void {
    const schedule = this.completingSchedule();
    if (!schedule) {
      return;
    }

    this.saving.set(true);
    this.formError.set(null);
    this.maintenanceService
      .completeSchedule(schedule.id, {
        completedOn: this.completeForm.completedOn,
        completedAtMileage: Number(this.completeForm.completedAtMileage),
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.completingSchedule.set(null);
          this.noticeMessage.set(`Completed schedule "${schedule.name}".`);
          this.loadSchedules();
        },
        error: (error: HttpErrorResponse) =>
          this.formError.set(getApiMessage(error, 'Failed to complete maintenance schedule.')),
      });
  }

  maintenanceTypeLabel(type: MaintenanceType): string {
    return this.maintenanceTypes.find((t) => t.value === type)?.label ?? 'Other';
  }

  scheduleStatusLabel(status: MaintenanceScheduleStatus): string {
    switch (status) {
      case MaintenanceScheduleStatus.Active:
        return 'Active';
      case MaintenanceScheduleStatus.Paused:
        return 'Paused';
      case MaintenanceScheduleStatus.Completed:
        return 'Completed';
      default:
        return 'Unknown';
    }
  }
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}
