import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { API_BASE_URL } from '../../core/api/api-config';
import { DriverManagementComponent } from './driver-management.component';

describe('DriverManagementComponent', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    window.sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [DriverManagementComponent],
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

  it('shows driver and license details while keeping mutations hidden from fleet managers', () => {
    window.sessionStorage.setItem('ethiofleet.session', JSON.stringify(sessionWithRoles(['FleetManager'])));
    const fixture = TestBed.createComponent(DriverManagementComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/drivers?page=1&pageSize=20').flush({
      items: [driverResponse(1, true)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    const rendered = fixture.nativeElement.textContent as string;
    expect(rendered).toContain('Hana Bekele');
    expect(rendered).toContain('hana@example.test');
    expect(rendered).toContain('+251911123456');
    expect(rendered).toContain('B');
    expect(rendered).toContain('Expired');
    expect(rendered).toContain('Active');
    expect(rendered).not.toContain('Add driver');
    expect(fixture.nativeElement.querySelector('select[aria-label^="Change status"]')).toBeNull();
    expect(rendered).not.toContain('Edit');
    expect(rendered).not.toContain('Remove');
  });

  it('creates a driver with trimmed contact details and optional emergency fields', () => {
    window.sessionStorage.setItem('ethiofleet.session', JSON.stringify(sessionWithRoles(['CompanyOwner'])));
    const fixture = TestBed.createComponent(DriverManagementComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/drivers?page=1&pageSize=20').flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.page-header .primary-button') as HTMLButtonElement).click();
    fixture.detectChanges();
    setInput(fixture, 'fullName', '  Hana Bekele  ');
    setInput(fixture, 'email', '  hana@example.test  ');
    setInput(fixture, 'phoneNumber', ' +251911123456 ');
    setInput(fixture, 'licenseNumber', ' AB-1234 ');
    setInput(fixture, 'licenseCategory', ' B ');
    setInput(fixture, 'licenseExpiryDate', '2027-01-01');
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.vehicle-form-section form') as HTMLFormElement)
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    const create = http.expectOne('http://api.test/api/v1/drivers');
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual({
      fullName: 'Hana Bekele',
      email: 'hana@example.test',
      phoneNumber: '+251911123456',
      licenseNumber: 'AB-1234',
      licenseCategory: 'B',
      licenseExpiryDate: '2027-01-01',
      emergencyContactName: null,
      emergencyContactPhone: null,
    });
    create.flush(driverResponse(1, false));
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/drivers?page=1&pageSize=20').flush({
      items: [driverResponse(1, false)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Driver added to the organization.');
    expect(fixture.nativeElement.textContent).toContain('Hana Bekele');
  });

  it('sends an owner-authorized status change and reloads the driver record', () => {
    window.sessionStorage.setItem('ethiofleet.session', JSON.stringify(sessionWithRoles(['CompanyOwner'])));
    const fixture = TestBed.createComponent(DriverManagementComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/drivers?page=1&pageSize=20').flush({
      items: [driverResponse(1, false)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    const select = fixture.nativeElement.querySelector(
      'select[aria-label="Change status for Hana Bekele"]',
    ) as HTMLSelectElement;
    select.value = '3';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.status-control button') as HTMLButtonElement).click();

    const request = http.expectOne('http://api.test/api/v1/drivers/driver-1/status');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ status: 3 });
    request.flush(driverResponse(3, false));
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/drivers?page=1&pageSize=20').flush({
      items: [driverResponse(3, false)],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Suspended');
  });
});

function sessionWithRoles(roles: string[]) {
  return {
    tokens: { accessToken: 'access-token', refreshToken: 'refresh-token' },
    user: {
      id: 'user-1',
      organizationId: 'organization-1',
      email: 'owner@example.test',
      userName: 'Fleet Owner',
      roles,
    },
  };
}

function driverResponse(status: number, licenseExpired: boolean) {
  return {
    id: 'driver-1',
    organizationId: 'organization-1',
    fullName: 'Hana Bekele',
    email: 'hana@example.test',
    phoneNumber: '+251911123456',
    emergencyContactName: null,
    emergencyContactPhone: null,
    licenseNumber: 'AB-1234',
    licenseCategory: 'B',
    licenseExpiryDate: '2027-01-01',
    licenseExpired,
    status,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function setInput(fixture: ComponentFixture<DriverManagementComponent>, name: string, value: string): void {
  const input = fixture.debugElement.query(By.css(`[name="${name}"]`));
  (input.nativeElement as HTMLInputElement).value = value;
  input.triggerEventHandler('ngModelChange', value);
}
