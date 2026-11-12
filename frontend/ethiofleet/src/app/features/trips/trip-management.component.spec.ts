import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { NgModel } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { API_BASE_URL } from '../../core/api/api-config';
import { TripManagementComponent } from './trip-management.component';
import { TripStatus } from '../../core/trips/trip.models';

describe('TripManagementComponent', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    window.sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [TripManagementComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: 'http://api.test/api/v1' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    window.sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it('creates a trip only after loading a vehicle current assignment', () => {
    window.sessionStorage.setItem(
      'ethiofleet.session',
      JSON.stringify(sessionWithRoles(['CompanyOwner'])),
    );
    const fixture = TestBed.createComponent(TripManagementComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/trips?page=1&pageSize=20').flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector('.page-header .primary-button') as HTMLButtonElement
    ).click();
    const vehiclesRequest = http.expectOne('http://api.test/api/v1/vehicles?page=1&pageSize=20');
    expect(vehiclesRequest.request.method).toBe('GET');
    vehiclesRequest.flush({
      items: [vehicleResponse()],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    fixture.debugElement
      .query(By.css('.trip-vehicle-select select'))
      .injector.get(NgModel)
      .viewToModelUpdate('vehicle-1');
    fixture.detectChanges();
    const assignmentRequest = http.expectOne(
      'http://api.test/api/v1/vehicles/vehicle-1/assignment',
    );
    expect(assignmentRequest.request.method).toBe('GET');
    assignmentRequest.flush(assignmentResponse());
    fixture.detectChanges();

    setInput(fixture, 'input[name="origin"]', 'Addis Ababa');
    setInput(fixture, 'input[name="destination"]', 'Adama');
    setInput(fixture, 'input[name="purpose"]', 'Delivery');
    setInput(fixture, 'input[name="plannedStart"]', '2026-11-13T06:00');
    fixture.detectChanges();
    const createButton = (fixture.nativeElement.querySelector('.trip-form-grid') as HTMLDivElement)
      .closest('form')!
      .querySelector('button[type="submit"]') as HTMLButtonElement;
    createButton.click();

    const createRequest = http.expectOne('http://api.test/api/v1/trips');
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toEqual({
      vehicleId: 'vehicle-1',
      driverId: 'driver-1',
      origin: 'Addis Ababa',
      destination: 'Adama',
      purpose: 'Delivery',
      plannedStart: new Date('2026-11-13T06:00').toISOString(),
      startMileage: 48000,
      notes: null,
    });
    createRequest.flush(tripResponse());
    fixture.detectChanges();
    http.expectOne('http://api.test/api/v1/trips?page=1&pageSize=20').flush({
      items: [tripResponse()],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Trip from Addis Ababa to Adama was scheduled.',
    );
  });

  it('completes an in-progress trip with a valid end time and odometer reading', () => {
    window.sessionStorage.setItem(
      'ethiofleet.session',
      JSON.stringify(sessionWithRoles(['FleetManager'])),
    );
    const fixture = TestBed.createComponent(TripManagementComponent);
    fixture.detectChanges();
    http.expectOne('http://api.test/api/v1/trips?page=1&pageSize=20').flush({
      items: [tripResponse(TripStatus.InProgress)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Schedule trip');
    (
      fixture.nativeElement.querySelector('.trip-actions .secondary-button') as HTMLButtonElement
    ).click();
    fixture.detectChanges();

    setInput(fixture, '.trip-completion-form input[name="actualEnd"]', '2026-11-12T23:00');
    setInput(fixture, '.trip-completion-form input[name="endMileage"]', '48120.5');
    fixture.detectChanges();
    (
      fixture.nativeElement.querySelector(
        '.trip-completion-form button[type="submit"]',
      ) as HTMLButtonElement
    ).click();

    const request = http.expectOne('http://api.test/api/v1/trips/trip-1/complete');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      actualEnd: new Date('2026-11-12T23:00').toISOString(),
      endMileage: 48120.5,
    });
    request.flush(tripResponse(TripStatus.Completed));
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/trips?page=1&pageSize=20').flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No trips match this view');
  });

  it('starts a planned trip through the trip lifecycle endpoint', () => {
    window.sessionStorage.setItem(
      'ethiofleet.session',
      JSON.stringify(sessionWithRoles(['FleetManager'])),
    );
    const fixture = TestBed.createComponent(TripManagementComponent);
    fixture.detectChanges();
    http.expectOne('http://api.test/api/v1/trips?page=1&pageSize=20').flush({
      items: [tripResponse()],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector('.trip-actions .secondary-button') as HTMLButtonElement
    ).click();
    const request = http.expectOne('http://api.test/api/v1/trips/trip-1/start');
    expect(request.request.method).toBe('POST');
    request.flush(tripResponse(TripStatus.InProgress));
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/trips?page=1&pageSize=20').flush({
      items: [tripResponse(TripStatus.InProgress)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('In progress');
    expect(fixture.nativeElement.textContent).not.toContain('Cancel trip');
  });

  it('does not allow scheduling when the selected vehicle has no active assignment', () => {
    window.sessionStorage.setItem(
      'ethiofleet.session',
      JSON.stringify(sessionWithRoles(['CompanyOwner'])),
    );
    const fixture = TestBed.createComponent(TripManagementComponent);
    fixture.detectChanges();
    http.expectOne('http://api.test/api/v1/trips?page=1&pageSize=20').flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector('.page-header .primary-button') as HTMLButtonElement
    ).click();
    http.expectOne('http://api.test/api/v1/vehicles?page=1&pageSize=20').flush({
      items: [vehicleResponse()],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();
    fixture.debugElement
      .query(By.css('.trip-vehicle-select select'))
      .injector.get(NgModel)
      .viewToModelUpdate('vehicle-1');
    fixture.detectChanges();
    http
      .expectOne('http://api.test/api/v1/vehicles/vehicle-1/assignment')
      .flush({ error: 'No active assignment.' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('no active driver assignment');
    const submitButton = (fixture.nativeElement.querySelector('.trip-form-grid') as HTMLDivElement)
      .closest('form')!
      .querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(submitButton.disabled).toBe(true);
  });
});

function setInput(
  fixture: ComponentFixture<TripManagementComponent>,
  selector: string,
  value: string,
): void {
  fixture.debugElement.query(By.css(selector)).injector.get(NgModel).viewToModelUpdate(value);
  fixture.detectChanges();
}

function sessionWithRoles(roles: string[]) {
  return {
    tokens: { accessToken: 'access-token', refreshToken: 'refresh-token' },
    user: {
      id: 'user-1',
      organizationId: 'organization-1',
      email: 'owner@example.test',
      userName: 'Fleet user',
      roles,
    },
  };
}

function vehicleResponse() {
  return {
    id: 'vehicle-1',
    organizationId: 'organization-1',
    plateNumber: 'ET-0148',
    make: 'Toyota',
    model: 'Hilux',
    year: 2022,
    type: 2,
    fuelType: 2,
    currentMileage: 48000,
    vin: null,
    status: 1,
    purchaseDate: null,
    currencyCode: 'ETB',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function assignmentResponse() {
  return {
    id: 'assignment-1',
    organizationId: 'organization-1',
    vehicleId: 'vehicle-1',
    driverId: 'driver-1',
    vehiclePlateNumber: 'ET-0148',
    driverFullName: 'Aster Bekele',
    assignedAt: '2026-03-01T08:00:00Z',
    unassignedAt: null,
    isActive: true,
    notes: null,
  };
}

function tripResponse(status = TripStatus.Planned) {
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
    plannedStart: '2026-11-12T18:00:00Z',
    actualStart: status === TripStatus.InProgress ? '2026-11-12T19:00:00Z' : null,
    actualEnd: status === TripStatus.Completed ? '2026-11-12T23:00:00Z' : null,
    startMileage: 48000,
    endMileage: status === TripStatus.Completed ? 48120.5 : null,
    distanceKm: status === TripStatus.Completed ? 120.5 : null,
    status,
    notes: null,
  };
}
