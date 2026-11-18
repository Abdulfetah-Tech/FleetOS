import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { NotificationPage } from './notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);
  private readonly notificationsUrl = `${this.apiBaseUrl}/notifications`;

  list(page: number, pageSize: number, unreadOnly = false): Observable<NotificationPage> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('unreadOnly', unreadOnly);
    return this.http.get<NotificationPage>(this.notificationsUrl, { params });
  }

  markRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.notificationsUrl}/${encodeURIComponent(id)}/read`, {});
  }
}
