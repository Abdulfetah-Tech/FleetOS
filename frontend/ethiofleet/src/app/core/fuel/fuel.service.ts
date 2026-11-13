import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { CreateFuelRecordRequest, FuelRecord, FuelRecordPage } from './fuel.models';

@Injectable({ providedIn: 'root' })
export class FuelService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/fuel-records`;

  list(page: number, pageSize: number, vehicleId: string | null = null, driverId: string | null = null): Observable<FuelRecordPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (vehicleId) params = params.set('vehicleId', vehicleId);
    if (driverId) params = params.set('driverId', driverId);
    return this.http.get<FuelRecordPage>(this.url, { params });
  }

  get(id: string): Observable<FuelRecord> {
    return this.http.get<FuelRecord>(this.recordUrl(id));
  }

  create(request: CreateFuelRecordRequest): Observable<FuelRecord> {
    return this.http.post<FuelRecord>(this.url, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(this.recordUrl(id));
  }

  private recordUrl(id: string): string {
    return `${this.url}/${encodeURIComponent(id)}`;
  }
}
