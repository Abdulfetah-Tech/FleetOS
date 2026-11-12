import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { AssignVehicleRequest, VehicleAssignment } from './vehicle-assignment.models';

@Injectable({ providedIn: 'root' })
export class VehicleAssignmentService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  getActive(vehicleId: string): Observable<VehicleAssignment> {
    return this.http.get<VehicleAssignment>(this.assignmentUrl(vehicleId));
  }

  assign(vehicleId: string, request: AssignVehicleRequest): Observable<VehicleAssignment> {
    return this.http.post<VehicleAssignment>(this.assignmentUrl(vehicleId), request);
  }

  unassign(vehicleId: string): Observable<VehicleAssignment> {
    return this.http.delete<VehicleAssignment>(this.assignmentUrl(vehicleId));
  }

  private assignmentUrl(vehicleId: string): string {
    return `${this.apiBaseUrl}/vehicles/${encodeURIComponent(vehicleId)}/assignment`;
  }
}
