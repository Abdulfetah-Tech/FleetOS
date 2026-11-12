import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { NgModel } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
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
    vi.restoreAllMocks();
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

  it('treats a missing assignment as unassigned without exposing owner actions to managers', () => {
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

    (fixture.nativeElement.querySelector(
      'button[aria-label="Manage assignment for ET-0148"]',
    ) as HTMLButtonElement).click();
    const assignmentRequest = http.expectOne('http://api.test/api/v1/vehicles/vehicle-1/assignment');
    expect(assignmentRequest.request.method).toBe('GET');
    assignmentRequest.flush({ error: 'No active assignment.' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Unassigned');
    expect(fixture.nativeElement.textContent).toContain('No active driver assignment.');
    expect(fixture.nativeElement.textContent).not.toContain('Assign a driver');
    expect(fixture.nativeElement.textContent).not.toContain('End assignment');
  });

  it('filters candidates, assigns an eligible driver, and ends the assignment', () => {
    window.sessionStorage.setItem('ethiofleet.session', JSON.stringify(sessionWithRoles(['CompanyOwner'])));
    const fixture = TestBed.createComponent(VehicleManagementComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/vehicles?page=1&pageSize=20').flush({
      items: [vehicleResponse(1)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    (fixture.nativeElement.querySelector(
      'button[aria-label="Manage assignment for ET-0148"]',
    ) as HTMLButtonElement).click();
    http.expectOne('http://api.test/api/v1/vehicles/vehicle-1/assignment').flush(
      { error: 'No active assignment.' },
      { status: 404, statusText: 'Not Found' },
    );
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.assignment-panel .primary-button') as HTMLButtonElement).click();
    fixture.detectChanges();
    const searchInput = fixture.nativeElement.querySelector('.assignment-search-form input') as HTMLInputElement;
    searchInput.value = 'Aster';
    fixture.debugElement.query(By.css('.assignment-search-form input'))
      .injector.get(NgModel).viewToModelUpdate('Aster');
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.assignment-search-form button') as HTMLButtonElement).click();

    const driverSearch = http.expectOne(
      'http://api.test/api/v1/drivers?page=1&pageSize=20&search=Aster',
    );
    expect(driverSearch.request.method).toBe('GET');
    driverSearch.flush({
      items: [
        driverResponse('driver-active', 1, false),
        driverResponse('driver-inactive', 2, false),
        driverResponse('driver-expired', 1, true),
      ],
      page: 1,
      pageSize: 20,
      totalCount: 3,
    });
    fixture.detectChanges();

    const driverSelect = fixture.nativeElement.querySelector(
      '.assignment-driver-select select',
    ) as HTMLSelectElement;
    expect(Array.from(driverSelect.options).map((option) => option.textContent)).toEqual([
      'Choose a driver',
      'Aster Bekele · B · 0911000000',
    ]);
    driverSelect.value = 'driver-active';
    fixture.debugElement.query(By.css('.assignment-driver-select select'))
      .injector.get(NgModel).viewToModelUpdate('driver-active');
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.assignment-form-actions .primary-button') as HTMLButtonElement).click();

    const assignRequest = http.expectOne('http://api.test/api/v1/vehicles/vehicle-1/assignment');
    expect(assignRequest.request.method).toBe('POST');
    expect(assignRequest.request.body).toEqual({
      vehicleId: 'vehicle-1',
      driverId: 'driver-active',
      notes: null,
    });
    assignRequest.flush(assignmentResponse());
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Aster Bekele');

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const endButton = buttons.find((button) => button.textContent?.includes('End assignment'));
    expect(endButton).toBeDefined();
    endButton?.click();
    const endRequest = http.expectOne('http://api.test/api/v1/vehicles/vehicle-1/assignment');
    expect(endRequest.request.method).toBe('DELETE');
    endRequest.flush({ ...assignmentResponse(), isActive: false, unassignedAt: '2026-03-01T10:00:00Z' });
    fixture.detectChanges();
    confirmSpy.mockRestore();

    expect(fixture.nativeElement.textContent).toContain('No active driver assignment.');
    expect(fixture.nativeElement.textContent).toContain('Assign a driver');
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

function driverResponse(id: string, status: number, licenseExpired: boolean) {
  return {
    id,
    organizationId: 'organization-1',
    fullName: 'Aster Bekele',
    email: 'aster@example.test',
    phoneNumber: '0911000000',
    emergencyContactName: null,
    emergencyContactPhone: null,
    licenseNumber: `ETH-${id}`,
    licenseCategory: 'B',
    licenseExpiryDate: licenseExpired ? '2025-01-01' : '2027-01-01',
    licenseExpired,
    status,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function assignmentResponse() {
  return {
    id: 'assignment-1',
    organizationId: 'organization-1',
    vehicleId: 'vehicle-1',
    driverId: 'driver-active',
    vehiclePlateNumber: 'ET-0148',
    driverFullName: 'Aster Bekele',
    assignedAt: '2026-03-01T08:00:00Z',
    unassignedAt: null,
    isActive: true,
    notes: null,
  };
}
