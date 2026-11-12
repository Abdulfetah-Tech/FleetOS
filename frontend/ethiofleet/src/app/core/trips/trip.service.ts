import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { CompleteTripRequest, CreateTripRequest, Trip, TripPage, TripStatus } from './trip.models';

@Injectable({ providedIn: 'root' })
export class TripService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);
  private readonly tripsUrl = `${this.apiBaseUrl}/trips`;

  list(page: number, pageSize: number, status: TripStatus | null): Observable<TripPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status !== null) {
      params = params.set('status', status);
    }
    return this.http.get<TripPage>(this.tripsUrl, { params });
  }

  create(request: CreateTripRequest): Observable<Trip> {
    return this.http.post<Trip>(this.tripsUrl, request);
  }

  start(id: string): Observable<Trip> {
    return this.http.post<Trip>(`${this.tripUrl(id)}/start`, {});
  }

  complete(id: string, request: CompleteTripRequest): Observable<Trip> {
    return this.http.post<Trip>(`${this.tripUrl(id)}/complete`, request);
  }

  cancel(id: string): Observable<Trip> {
    return this.http.post<Trip>(`${this.tripUrl(id)}/cancel`, {});
  }

  private tripUrl(id: string): string {
    return `${this.tripsUrl}/${encodeURIComponent(id)}`;
  }
}
