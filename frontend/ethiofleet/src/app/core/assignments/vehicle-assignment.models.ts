export interface VehicleAssignment {
  id: string;
  organizationId: string;
  vehicleId: string;
  driverId: string;
  vehiclePlateNumber: string;
  driverFullName: string;
  assignedAt: string;
  unassignedAt: string | null;
  isActive: boolean;
  notes: string | null;
}

export interface AssignVehicleRequest {
  vehicleId: string;
  driverId: string;
  notes: string | null;
}
