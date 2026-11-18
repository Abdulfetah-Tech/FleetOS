export enum MaintenanceType {
  Preventive = 1,
  Corrective = 2,
  Repair = 3,
  Inspection = 4,
  Other = 5,
}

export enum MaintenanceScheduleStatus {
  Active = 1,
  Paused = 2,
  Completed = 3,
}

export interface MaintenanceRecord {
  id: string;
  organizationId: string;
  vehicleId: string;
  vehiclePlateNumber: string;
  type: MaintenanceType;
  description: string;
  performedOn: string;
  odometer: number;
  laborCost: number;
  partsCost: number;
  totalCost: number;
  currencyCode: string;
  serviceProvider: string | null;
  reference: string | null;
  notes: string | null;
}

export interface MaintenanceRecordPage {
  items: MaintenanceRecord[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateMaintenanceRecordRequest {
  vehicleId: string;
  type: MaintenanceType;
  description: string;
  performedOn: string;
  odometer: number;
  laborCost: number;
  partsCost: number;
  currencyCode: string;
  serviceProvider: string | null;
  reference: string | null;
  notes: string | null;
}

export interface MaintenanceSchedule {
  id: string;
  organizationId: string;
  vehicleId: string;
  vehiclePlateNumber: string;
  name: string;
  description: string | null;
  nextDueDate: string | null;
  nextDueMileage: number | null;
  intervalDays: number | null;
  intervalMileage: number | null;
  status: MaintenanceScheduleStatus;
  isDue: boolean;
}

export interface MaintenanceSchedulePage {
  items: MaintenanceSchedule[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateMaintenanceScheduleRequest {
  vehicleId: string;
  name: string;
  description: string | null;
  nextDueDate: string | null;
  nextDueMileage: number | null;
  intervalDays: number | null;
  intervalMileage: number | null;
}

export interface CompleteMaintenanceScheduleRequest {
  completedOn: string;
  completedAtMileage: number;
}

export const MAINTENANCE_TYPES = [
  { value: MaintenanceType.Preventive, label: 'Preventive' },
  { value: MaintenanceType.Corrective, label: 'Corrective' },
  { value: MaintenanceType.Repair, label: 'Repair' },
  { value: MaintenanceType.Inspection, label: 'Inspection' },
  { value: MaintenanceType.Other, label: 'Other' },
] as const;
