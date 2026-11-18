export interface CurrencyCostSummary {
  currencyCode: string;
  fuelCost: number;
  maintenanceCost: number;
  totalCost: number;
}

export interface VehicleCostRow {
  vehicleId: string;
  plateNumber: string;
  currencyCode: string;
  fuelCost: number;
  maintenanceCost: number;
  fuelRecordCount: number;
  maintenanceRecordCount: number;
}

export interface VehicleUtilizationRow {
  vehicleId: string;
  plateNumber: string;
  completedTrips: number;
  distanceKm: number;
}

export interface FleetReport {
  from: string;
  to: string;
  costsByCurrency: CurrencyCostSummary[];
  vehicleCosts: VehicleCostRow[];
  utilization: VehicleUtilizationRow[];
}
