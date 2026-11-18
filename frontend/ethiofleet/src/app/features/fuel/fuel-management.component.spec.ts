import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NgModel } from '@angular/forms';
import { API_BASE_URL } from '../../core/api/api-config';
import { FuelManagementComponent } from './fuel-management.component';

describe('FuelManagementComponent', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    window.sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [FuelManagementComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.test/api/v1' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    window.sessionStorage.clear();
  });

  it('keeps create and remove actions hidden from fleet managers', () => {
    window.sessionStorage.setItem(
      'ethiofleet.session',
      JSON.stringify(sessionWithRoles(['FleetManager'])),
    );
    const fixture = TestBed.createComponent(FuelManagementComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/fuel-records?page=1&pageSize=20').flush({
      items: [fuelResponse()],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    const rendered = fixture.nativeElement.textContent as string;
    expect(rendered).toContain('ET-3-12345');
    expect(rendered).toContain('50.5 L');
    expect(rendered).not.toContain('Log fuel');
    expect(rendered).not.toContain('Remove');
  });

  it('creates a fuel record after loading vehicles and the active assignment', () => {
    window.sessionStorage.setItem(
      'ethiofleet.session',
      JSON.stringify(sessionWithRoles(['CompanyOwner'])),
    );
    const fixture = TestBed.createComponent(FuelManagementComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/fuel-records?page=1&pageSize=20').flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector('.page-header .primary-button') as HTMLButtonElement
    ).click();
    http.expectOne('http://api.test/api/v1/vehicles?page=1&pageSize=25').flush({
      items: [vehicleResponse()],
      page: 1,
      pageSize: 25,
      totalCount: 1,
    });
    fixture.detectChanges();

    fixture.debugElement
      .query(By.css('select[name="vehicleId"]'))
      .injector.get(NgModel)
      .viewToModelUpdate('vehicle-1');
    fixture.detectChanges();
    setInput(fixture, 'quantityLiters', '40');
    setInput(fixture, 'unitPrice', '90');
    setInput(fixture, 'odometer', '12550');
    (
      fixture.nativeElement.querySelector('.vehicle-form-section form') as HTMLFormElement
    ).dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/vehicles/vehicle-1/assignment').flush({
      driverId: 'driver-1',
    });
    const create = http.expectOne('http://api.test/api/v1/fuel-records');
    expect(create.request.method).toBe('POST');
    expect(create.request.body.vehicleId).toBe('vehicle-1');
    expect(create.request.body.driverId).toBe('driver-1');
    expect(create.request.body.quantityLiters).toBe(40);
    expect(create.request.body.currencyCode).toBe('ETB');
    create.flush(fuelResponse());
    http.expectOne('http://api.test/api/v1/fuel-records?page=1&pageSize=20').flush({
      items: [fuelResponse()],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
  });
});

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
    plateNumber: 'ET-3-12345',
    make: 'Isuzu',
    model: 'NPR',
    year: 2021,
    type: 4,
    fuelType: 2,
    currentMileage: 12500,
    vin: null,
    status: 1,
    purchaseDate: null,
    currencyCode: 'ETB',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function fuelResponse() {
  return {
    id: 'fuel-1',
    organizationId: 'organization-1',
    vehicleId: 'vehicle-1',
    vehiclePlateNumber: 'ET-3-12345',
    driverId: 'driver-1',
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
    notes: null,
  };
}

function setInput(
  fixture: ComponentFixture<FuelManagementComponent>,
  name: string,
  value: string,
): void {
  const input = fixture.debugElement.query(By.css(`[name="${name}"]`));
  (input.nativeElement as HTMLInputElement).value = value;
  input.triggerEventHandler('ngModelChange', Number(value));
}
