export interface FuelRecord {
  id: string;
  organizationId: string;
  vehicleId: string;
  vehiclePlateNumber: string;
  driverId: string | null;
  driverName: string | null;
  tripId: string | null;
  occurredAt: string;
  quantityLiters: number;
  unitPrice: number;
  totalCost: number;
  odometer: number;
  station: string | null;
  currencyCode: string;
  receiptReference: string | null;
  notes: string | null;
}

export interface FuelRecordPage {
  items: FuelRecord[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateFuelRecordRequest {
  vehicleId: string;
  driverId: string | null;
  tripId: string | null;
  occurredAt: string;
  quantityLiters: number;
  unitPrice: number;
  odometer: number;
  station: string | null;
  currencyCode: string;
  receiptReference: string | null;
  notes: string | null;
}
