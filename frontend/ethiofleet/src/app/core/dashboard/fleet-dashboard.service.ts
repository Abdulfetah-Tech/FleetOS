import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { FleetDashboardResponse } from './fleet-dashboard.models';

@Injectable({ providedIn: 'root' })
export class FleetDashboardService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  getFleetDashboard(): Observable<FleetDashboardResponse> {
    return this.http.get<FleetDashboardResponse>(`${this.apiBaseUrl}/dashboard/fleet`);
  }
}
