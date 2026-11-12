import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import {
  CreateDriverRequest,
  Driver,
  DriverPage,
  DriverStatus,
  UpdateDriverRequest,
} from './driver.models';

@Injectable({ providedIn: 'root' })
export class DriverService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);
  private readonly driversUrl = `${this.apiBaseUrl}/drivers`;

  list(page: number, pageSize: number, search: string): Observable<DriverPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    const query = search.trim();
    if (query) {
      params = params.set('search', query);
    }
    return this.http.get<DriverPage>(this.driversUrl, { params });
  }

  create(request: CreateDriverRequest): Observable<Driver> {
    return this.http.post<Driver>(this.driversUrl, request);
  }

  update(id: string, request: UpdateDriverRequest): Observable<Driver> {
    return this.http.put<Driver>(`${this.driversUrl}/${encodeURIComponent(id)}`, request);
  }

  changeStatus(id: string, status: DriverStatus): Observable<Driver> {
    return this.http.patch<Driver>(`${this.driversUrl}/${encodeURIComponent(id)}/status`, { status });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.driversUrl}/${encodeURIComponent(id)}`);
  }
}
