import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../../core/api/api-config';
import { VehicleManagementComponent } from './vehicle-management.component';

describe('VehicleManagementComponent', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    window.sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [VehicleManagementComponent],
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

  it('keeps owner-only create, edit, and remove actions hidden from fleet managers', () => {
    window.sessionStorage.setItem('ethiofleet.session', JSON.stringify(sessionWithRoles(['FleetManager'])));
    const fixture = TestBed.createComponent(VehicleManagementComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/vehicles?page=1&pageSize=20').flush({
      items: [vehicleResponse(1)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    const rendered = fixture.nativeElement.textContent as string;
    expect(rendered).toContain('ET-0148');
    expect(rendered).toContain('Active');
    expect(rendered).not.toContain('Add vehicle');
    expect(rendered).not.toContain('Edit');
    expect(rendered).not.toContain('Remove');
  });

  it('enables a changed status and sends the selected status to the API', () => {
    window.sessionStorage.setItem('ethiofleet.session', JSON.stringify(sessionWithRoles(['FleetManager'])));
    const fixture = TestBed.createComponent(VehicleManagementComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/vehicles?page=1&pageSize=20').flush({
      items: [vehicleResponse(1)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    const select = fixture.nativeElement.querySelector(
      'select[aria-label="Change status for ET-0148"]',
    ) as HTMLSelectElement;
    select.value = '3';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const updateButton = fixture.nativeElement.querySelector('.status-control button') as HTMLButtonElement;
    expect(updateButton.disabled).toBe(false);
    updateButton.click();

    const statusRequest = http.expectOne('http://api.test/api/v1/vehicles/vehicle-1/status');
    expect(statusRequest.request.method).toBe('PATCH');
    expect(statusRequest.request.body).toEqual({ status: 3 });
    statusRequest.flush(vehicleResponse(3));
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/vehicles?page=1&pageSize=20').flush({
      items: [vehicleResponse(3)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Maintenance');
  });
});

function sessionWithRoles(roles: string[]) {
  return {
    tokens: { accessToken: 'access-token', refreshToken: 'refresh-token' },
    user: {
      id: 'user-1',
      organizationId: 'organization-1',
      email: 'manager@example.test',
      userName: 'Fleet Manager',
      roles,
    },
  };
}

function vehicleResponse(status: number) {
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
    status,
    purchaseDate: null,
    currencyCode: 'ETB',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}
