import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import {
  CreateVehicleRequest,
  UpdateVehicleRequest,
  Vehicle,
  VehiclePage,
  VehicleStatus,
} from './vehicle.models';

@Injectable({ providedIn: 'root' })
export class VehicleService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);
  private readonly vehiclesUrl = `${this.apiBaseUrl}/vehicles`;

  list(page: number, pageSize: number, search: string): Observable<VehiclePage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    const query = search.trim();
    if (query) {
      params = params.set('search', query);
    }
    return this.http.get<VehiclePage>(this.vehiclesUrl, { params });
  }

  create(request: CreateVehicleRequest): Observable<Vehicle> {
    return this.http.post<Vehicle>(this.vehiclesUrl, request);
  }

  update(id: string, request: UpdateVehicleRequest): Observable<Vehicle> {
    return this.http.put<Vehicle>(`${this.vehiclesUrl}/${encodeURIComponent(id)}`, request);
  }

  changeStatus(id: string, status: VehicleStatus): Observable<Vehicle> {
    return this.http.patch<Vehicle>(`${this.vehiclesUrl}/${encodeURIComponent(id)}/status`, { status });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.vehiclesUrl}/${encodeURIComponent(id)}`);
  }
}
