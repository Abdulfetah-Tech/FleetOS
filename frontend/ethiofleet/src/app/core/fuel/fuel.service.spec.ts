import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../api/api-config';
import { CreateFuelRecordRequest } from './fuel.models';
import { FuelService } from './fuel.service';

describe('FuelService', () => {
  let service: FuelService;
  let http: HttpTestingController;
  const url = 'http://api.test/api/v1/fuel-records';
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), { provide: API_BASE_URL, useValue: 'http://api.test/api/v1' }] });
    service = TestBed.inject(FuelService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('lists paginated records without optional filters', () => {
    service.list(2, 20).subscribe(result => expect(result.totalCount).toBe(21));
    const request = http.expectOne(`${url}?page=2&pageSize=20`);
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], page: 2, pageSize: 20, totalCount: 21 });
  });

  it('encodes vehicle and driver filters', () => {
    service.list(1, 10, 'vehicle & 1', 'driver/1').subscribe();
    const request = http.expectOne(req => req.url === url);
    expect(request.request.params.get('vehicleId')).toBe('vehicle & 1');
    expect(request.request.params.get('driverId')).toBe('driver/1');
    expect(request.request.params.get('pageSize')).toBe('10');
    request.flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });
  });

  it('gets a record with an encoded identifier', () => {
    service.get('fuel/1').subscribe();
    const request = http.expectOne(`${url}/fuel%2F1`);
    expect(request.request.method).toBe('GET');
    request.flush({ id: 'fuel/1' });
  });

  it('posts the complete typed payload and accepts the created record', () => {
    const body: CreateFuelRecordRequest = { vehicleId: 'vehicle-1', driverId: null, tripId: null, occurredAt: '2026-11-13T06:00:00.000Z', quantityLiters: 12.345, unitPrice: 100.25, odometer: 48000.1, currencyCode: 'ETB', station: null, receiptReference: 'R-1', notes: null };
    service.create(body).subscribe(record => expect(record.id).toBe('fuel-1'));
    const request = http.expectOne(url);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ ...body, id: 'fuel-1' }, { status: 201, statusText: 'Created' });
  });

  it('deletes using an encoded identifier and handles 204', () => {
    service.delete('fuel/1').subscribe(result => expect(result).toBeNull());
    const request = http.expectOne(`${url}/fuel%2F1`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('propagates server errors instead of returning fake records', () => {
    service.list(1, 20).subscribe({ next: () => { throw new Error('Unexpected success'); }, error: (error: HttpErrorResponse) => expect(error.status).toBe(403) });
    http.expectOne(`${url}?page=1&pageSize=20`).flush({ error: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });
  });
});
