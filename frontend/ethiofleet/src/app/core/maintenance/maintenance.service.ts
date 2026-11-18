import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import {
  CompleteMaintenanceScheduleRequest,
  CreateMaintenanceRecordRequest,
  CreateMaintenanceScheduleRequest,
  MaintenanceRecord,
  MaintenanceRecordPage,
  MaintenanceSchedule,
  MaintenanceSchedulePage,
} from './maintenance.models';

@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);
  private readonly recordsUrl = `${this.apiBaseUrl}/maintenance/records`;
  private readonly schedulesUrl = `${this.apiBaseUrl}/maintenance/schedules`;

  listRecords(
    page: number,
    pageSize: number,
    vehicleId?: string | null,
  ): Observable<MaintenanceRecordPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (vehicleId) {
      params = params.set('vehicleId', vehicleId);
    }
    return this.http.get<MaintenanceRecordPage>(this.recordsUrl, { params });
  }

  createRecord(request: CreateMaintenanceRecordRequest): Observable<MaintenanceRecord> {
    return this.http.post<MaintenanceRecord>(this.recordsUrl, request);
  }

  listSchedules(
    page: number,
    pageSize: number,
    vehicleId?: string | null,
    dueOnly = false,
  ): Observable<MaintenanceSchedulePage> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('dueOnly', dueOnly);
    if (vehicleId) {
      params = params.set('vehicleId', vehicleId);
    }
    return this.http.get<MaintenanceSchedulePage>(this.schedulesUrl, { params });
  }

  createSchedule(request: CreateMaintenanceScheduleRequest): Observable<MaintenanceSchedule> {
    return this.http.post<MaintenanceSchedule>(this.schedulesUrl, request);
  }

  completeSchedule(
    id: string,
    request: CompleteMaintenanceScheduleRequest,
  ): Observable<MaintenanceSchedule> {
    return this.http.post<MaintenanceSchedule>(
      `${this.schedulesUrl}/${encodeURIComponent(id)}/complete`,
      request,
    );
  }
}
