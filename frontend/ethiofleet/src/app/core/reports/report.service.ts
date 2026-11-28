import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { FleetReport } from './report.models';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);
  private readonly reportsUrl = `${this.apiBaseUrl}/reports/fleet`;

  get(from: string, to: string): Observable<FleetReport> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<FleetReport>(this.reportsUrl, { params });
  }

  exportCsv(from: string, to: string): Observable<Blob> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get(`${this.reportsUrl}/export.csv`, {
      params,
      responseType: 'blob',
    });
  }
}
