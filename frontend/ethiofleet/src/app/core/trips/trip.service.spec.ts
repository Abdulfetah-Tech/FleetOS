import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../api/api-config';
import { TripService } from './trip.service';
import { TripStatus } from './trip.models';

describe('TripService', () => {
  let service: TripService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.test/api/v1' },
      ],
    });
    service = TestBed.inject(TripService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists trips with status filters and bounded pagination parameters', () => {
    service.list(2, 20, TripStatus.InProgress).subscribe();

    const request = http.expectOne('http://api.test/api/v1/trips?page=2&pageSize=20&status=2');
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], page: 2, pageSize: 20, totalCount: 0 });
  });

  it('uses the trip lifecycle endpoints and sends completion measurements', () => {
    service
      .create({
        vehicleId: 'vehicle-1',
        driverId: 'driver-1',
        origin: 'Addis Ababa',
        destination: 'Adama',
        purpose: 'Delivery',
        plannedStart: '2026-11-13T06:00:00Z',
        startMileage: 48000,
        notes: null,
      })
      .subscribe();
    const create = http.expectOne('http://api.test/api/v1/trips');
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toMatchObject({
      vehicleId: 'vehicle-1',
      driverId: 'driver-1',
      startMileage: 48000,
    });
    create.flush(tripResponse());

    service.start('trip-1').subscribe();
    const start = http.expectOne('http://api.test/api/v1/trips/trip-1/start');
    expect(start.request.method).toBe('POST');
    start.flush(tripResponse());

    service
      .complete('trip-1', { actualEnd: '2026-11-13T09:00:00Z', endMileage: 48120.5 })
      .subscribe();
    const complete = http.expectOne('http://api.test/api/v1/trips/trip-1/complete');
    expect(complete.request.method).toBe('POST');
    expect(complete.request.body).toEqual({
      actualEnd: '2026-11-13T09:00:00Z',
      endMileage: 48120.5,
    });
    complete.flush(tripResponse());

    service.cancel('trip-1').subscribe();
    const cancel = http.expectOne('http://api.test/api/v1/trips/trip-1/cancel');
    expect(cancel.request.method).toBe('POST');
    cancel.flush(tripResponse());
  });
});

function tripResponse() {
  return {
    id: 'trip-1',
    organizationId: 'organization-1',
    vehicleId: 'vehicle-1',
    vehiclePlateNumber: 'ET-0148',
    driverId: 'driver-1',
    driverFullName: 'Aster Bekele',
    vehicleAssignmentId: 'assignment-1',
    origin: 'Addis Ababa',
    destination: 'Adama',
    purpose: 'Delivery',
    plannedStart: '2026-11-13T06:00:00Z',
    actualStart: null,
    actualEnd: null,
    startMileage: 48000,
    endMileage: null,
    distanceKm: null,
    status: TripStatus.Planned,
    notes: null,
  };
}
