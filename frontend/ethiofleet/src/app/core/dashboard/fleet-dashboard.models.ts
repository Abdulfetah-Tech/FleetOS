export interface DashboardCurrencyTotal {
  currencyCode: string;
  fuelCost: number;
  maintenanceCost: number;
  operatingCost: number;
}

export interface UpcomingDocumentExpiry {
  documentId: string;
  vehicleId: string;
  plateNumber: string;
  name: string;
  expiresOn: string;
  daysUntilExpiry: number;
}

export interface DueMaintenanceSchedule {
  scheduleId: string;
  vehicleId: string;
  plateNumber: string;
  name: string;
  dueDate: string | null;
  dueMileage: number | null;
  currentMileage: number;
}

export interface FleetDashboardResponse {
  asOfDate: string;
  month: string;
  totalVehicles: number;
  activeVehicles: number;
  vehiclesInMaintenance: number;
  availableVehicles: number;
  totalDrivers: number;
  activeDrivers: number;
  tripsToday: number;
  monthlyCosts: DashboardCurrencyTotal[];
  upcomingDocumentExpiries: UpcomingDocumentExpiry[];
  dueMaintenance: DueMaintenanceSchedule[];
  openIncidents: number;
}
