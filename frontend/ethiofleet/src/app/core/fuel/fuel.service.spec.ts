import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../api/api-config';
import { FuelService } from './fuel.service';
import { FuelRecord } from './fuel.models';

describe('FuelService', () => {
  let service: FuelService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.test/api/v1' },
      ],
    });
    service = TestBed.inject(FuelService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists fuel records with vehicle filter and pagination parameters', () => {
    service.list(2, 25, 'vehicle-123', 'driver-456').subscribe();

    const request = http.expectOne(
      'http://api.test/api/v1/fuel-records?page=2&pageSize=25&vehicleId=vehicle-123&driverId=driver-456',
    );
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], page: 2, pageSize: 25, totalCount: 0 });
  });

  it('creates and deletes fuel records through the API endpoints', () => {
    const payload = {
      vehicleId: 'v-1',
      driverId: 'd-1',
      tripId: null,
      occurredAt: '2026-11-13T10:00:00Z',
      quantityLiters: 50.5,
      unitPrice: 82.5,
      odometer: 12500,
      station: 'TotalEnergies Bole',
      currencyCode: 'ETB',
      receiptReference: 'REC-9912',
      notes: 'Full tank fill-up',
    };

    service.create(payload).subscribe();
    const createReq = http.expectOne('http://api.test/api/v1/fuel-records');
    expect(createReq.request.method).toBe('POST');
    expect(createReq.request.body).toEqual(payload);
    createReq.flush(dummyFuelRecord());

    service.delete('fuel-1').subscribe();
    const deleteReq = http.expectOne('http://api.test/api/v1/fuel-records/fuel-1');
    expect(deleteReq.request.method).toBe('DELETE');
    deleteReq.flush(null);
  });
});

function dummyFuelRecord(): FuelRecord {
  return {
    id: 'fuel-1',
    organizationId: 'org-1',
    vehicleId: 'v-1',
    vehiclePlateNumber: 'ET-3-12345',
    driverId: 'd-1',
    driverName: 'Almaz Tadesse',
    tripId: null,
    occurredAt: '2026-11-13T10:00:00Z',
    quantityLiters: 50.5,
    unitPrice: 82.5,
    totalCost: 4166.25,
    odometer: 12500,
    station: 'TotalEnergies Bole',
    currencyCode: 'ETB',
    receiptReference: 'REC-9912',
    notes: 'Full tank fill-up',
  };
}
