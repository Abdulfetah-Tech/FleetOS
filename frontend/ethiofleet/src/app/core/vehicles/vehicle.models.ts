export enum VehicleType {
  Car = 1,
  Pickup = 2,
  Van = 3,
  Truck = 4,
  Bus = 5,
  Motorcycle = 6,
  HeavyEquipment = 7,
  Other = 8,
}

export enum FuelType {
  Petrol = 1,
  Diesel = 2,
  Hybrid = 3,
  Electric = 4,
  Lpg = 5,
  Other = 6,
}

export enum VehicleStatus {
  Active = 1,
  Inactive = 2,
  Maintenance = 3,
  OutOfService = 4,
  Retired = 5,
}

export interface Vehicle {
  id: string;
  organizationId: string;
  plateNumber: string;
  make: string;
  model: string;
  year: number;
  type: VehicleType;
  fuelType: FuelType;
  currentMileage: number;
  vin: string | null;
  status: VehicleStatus;
  purchaseDate: string | null;
  currencyCode: string;
  createdAt: string;
  updatedAt: string;
}

export interface VehiclePage {
  items: Vehicle[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateVehicleRequest {
  plateNumber: string;
  make: string;
  model: string;
  year: number;
  type: VehicleType;
  fuelType: FuelType;
  currentMileage: number;
  vin: string | null;
  currencyCode: string;
  purchaseDate: string | null;
}

export type UpdateVehicleRequest = Omit<CreateVehicleRequest, 'currentMileage' | 'currencyCode'>;

export const VEHICLE_TYPES = [
  { value: VehicleType.Car, label: 'Car' },
  { value: VehicleType.Pickup, label: 'Pickup' },
  { value: VehicleType.Van, label: 'Van' },
  { value: VehicleType.Truck, label: 'Truck' },
  { value: VehicleType.Bus, label: 'Bus' },
  { value: VehicleType.Motorcycle, label: 'Motorcycle' },
  { value: VehicleType.HeavyEquipment, label: 'Heavy equipment' },
  { value: VehicleType.Other, label: 'Other' },
] as const;

export const FUEL_TYPES = [
  { value: FuelType.Petrol, label: 'Petrol' },
  { value: FuelType.Diesel, label: 'Diesel' },
  { value: FuelType.Hybrid, label: 'Hybrid' },
  { value: FuelType.Electric, label: 'Electric' },
  { value: FuelType.Lpg, label: 'LPG' },
  { value: FuelType.Other, label: 'Other' },
] as const;

export const VEHICLE_STATUSES = [
  { value: VehicleStatus.Active, label: 'Active' },
  { value: VehicleStatus.Inactive, label: 'Inactive' },
  { value: VehicleStatus.Maintenance, label: 'Maintenance' },
  { value: VehicleStatus.OutOfService, label: 'Out of service' },
  { value: VehicleStatus.Retired, label: 'Retired' },
] as const;
