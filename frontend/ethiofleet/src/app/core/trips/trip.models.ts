export enum TripStatus {
  Planned = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4,
}

export interface Trip {
  id: string;
  organizationId: string;
  vehicleId: string;
  vehiclePlateNumber: string;
  driverId: string;
  driverFullName: string;
  vehicleAssignmentId: string;
  origin: string;
  destination: string;
  purpose: string;
  plannedStart: string;
  actualStart: string | null;
  actualEnd: string | null;
  startMileage: number;
  endMileage: number | null;
  distanceKm: number | null;
  status: TripStatus;
  notes: string | null;
}

export interface TripPage {
  items: Trip[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateTripRequest {
  vehicleId: string;
  driverId: string;
  origin: string;
  destination: string;
  purpose: string;
  plannedStart: string;
  startMileage: number;
  notes: string | null;
}

export interface CompleteTripRequest {
  actualEnd: string;
  endMileage: number;
}

export const TRIP_STATUSES = [
  { value: TripStatus.Planned, label: 'Planned' },
  { value: TripStatus.InProgress, label: 'In progress' },
  { value: TripStatus.Completed, label: 'Completed' },
  { value: TripStatus.Cancelled, label: 'Cancelled' },
] as const;
