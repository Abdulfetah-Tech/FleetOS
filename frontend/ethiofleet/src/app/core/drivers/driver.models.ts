export enum DriverStatus {
  Active = 1,
  Inactive = 2,
  Suspended = 3,
}

export interface Driver {
  id: string;
  organizationId: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  licenseNumber: string;
  licenseCategory: string;
  licenseExpiryDate: string;
  licenseExpired: boolean;
  status: DriverStatus;
  createdAt: string;
  updatedAt: string;
}

export interface DriverPage {
  items: Driver[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateDriverRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  licenseNumber: string;
  licenseCategory: string;
  licenseExpiryDate: string;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
}

export type UpdateDriverRequest = CreateDriverRequest;

export const DRIVER_STATUSES = [
  { value: DriverStatus.Active, label: 'Active' },
  { value: DriverStatus.Inactive, label: 'Inactive' },
  { value: DriverStatus.Suspended, label: 'Suspended' },
] as const;
