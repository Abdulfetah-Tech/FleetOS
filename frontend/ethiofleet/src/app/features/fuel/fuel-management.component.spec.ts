import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { vi } from 'vitest';
import { API_BASE_URL } from '../../core/api/api-config';
import { AuthService } from '../../core/auth/auth.service';
import { FuelRecord } from '../../core/fuel/fuel.models';
import { FuelType, Vehicle, VehicleStatus, VehicleType } from '../../core/vehicles/vehicle.models';
import { FuelManagementComponent, localDateTimeInput } from './fuel-management.component';

describe('FuelManagementComponent', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<FuelManagementComponent>;
  const role = signal('CompanyOwner');
  const base = 'http://api.test/api/v1';
  const fuelUrl = `${base}/fuel-records`;
  const vehicle: Vehicle = { id: 'vehicle-1', organizationId: 'org-1', plateNumber: 'AA-123', make: 'Toyota', model: 'Hilux', year: 2024, type: VehicleType.Pickup, fuelType: FuelType.Diesel, currentMileage: 48000, vin: null, status: VehicleStatus.Active, purchaseDate: null, currencyCode: 'ETB', createdAt: '', updatedAt: '' };
  const record: FuelRecord = { id: 'fuel-1', organizationId: 'org-1', vehicleId: vehicle.id, vehiclePlateNumber: vehicle.plateNumber, driverId: null, driverName: null, tripId: null, occurredAt: '2026-11-13T06:00:00Z', quantityLiters: 12.345, unitPrice: 100.25, totalCost: 1237.59, odometer: 48000.1, currencyCode: 'ETB', station: 'Station A', receiptReference: 'R-1', notes: 'Purchase' };

  beforeEach(() => {
    role.set('CompanyOwner');
    TestBed.configureTestingModule({ imports: [FuelManagementComponent], providers: [provideHttpClient(), provideHttpClientTesting(), { provide: API_BASE_URL, useValue: base }, { provide: AuthService, useValue: { currentSession: () => ({ user: { roles: role() ? [role()] : [] } }) } }] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); vi.restoreAllMocks(); });

  function start(items: FuelRecord[] = [], totalCount = items.length): FuelManagementComponent {
    fixture = TestBed.createComponent(FuelManagementComponent);
    fixture.detectChanges();
    if (['CompanyOwner', 'CompanyAdministrator', 'FleetManager'].includes(role())) {
      http.expectOne(`${fuelUrl}?page=1&pageSize=20`).flush({ items, page: 1, pageSize: 20, totalCount });
    }
    fixture.detectChanges();
    return fixture.componentInstance;
  }
  function chooseCreateVehicle(component: FuelManagementComponent): void {
    component['beginCreate']();
    http.expectOne(`${base}/vehicles?page=1&pageSize=20`).flush({ items: [vehicle], page: 1, pageSize: 20, totalCount: 1 });
    component['selectVehicle'](vehicle);
    fixture.detectChanges();
  }
  function validForm(component: FuelManagementComponent): void {
    component['form'].patchValue({ occurredAt: '2026-11-13T09:00', quantityLiters: 12.345, unitPrice: 100.25, odometer: 48000.1, station: ' Station A ', receiptReference: ' R-1 ', notes: '  ' });
  }

  it('shows explicit loading, empty, error and retry states without fake data', () => {
    fixture = TestBed.createComponent(FuelManagementComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Loading fuel records');
    http.expectOne(`${fuelUrl}?page=1&pageSize=20`).flush(null, { status: 0, statusText: 'Unknown Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('could not be reached');
    (fixture.nativeElement.querySelector('.load-error button') as HTMLButtonElement).click();
    http.expectOne(`${fuelUrl}?page=1&pageSize=20`).flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No fuel records found');
  });

  it.each(['FleetManager', 'Driver', ''])('denies mutation methods and hides controls for %s without HTTP actions', deniedRole => {
    role.set(deniedRole);
    const component = start([record]);
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    component['beginCreate']();
    component['createRecord']();
    component['deleteRecord'](record);
    component['openPicker']('create');
    expect(confirm).not.toHaveBeenCalled();
    http.expectNone(request => request.method !== 'GET');
    http.expectNone(request => request.url === `${base}/vehicles`);
    expect(fixture.nativeElement.querySelector('.page-header .primary-button')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Delete');
    if (deniedRole !== 'FleetManager') {
      component['loadRecords']();
      component['openPicker']('filter');
      http.expectNone(() => true);
      expect(fixture.nativeElement.textContent).toContain('does not have permission');
    }
  });

  it('allows the CompanyAdministrator owner alias to create with local-time conversion and trimmed optional fields', () => {
    role.set('CompanyAdministrator');
    const component = start();
    chooseCreateVehicle(component);
    expect(component['form'].controls.currencyCode.value).toBe('ETB');
    validForm(component);
    fixture.detectChanges();
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    const request = http.expectOne(fuelUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ vehicleId: vehicle.id, driverId: null, tripId: null, occurredAt: new Date('2026-11-13T09:00').toISOString(), quantityLiters: 12.345, unitPrice: 100.25, odometer: 48000.1, currencyCode: 'ETB', station: 'Station A', receiptReference: 'R-1', notes: null });
    component['createRecord'](); // A second click while saving must not duplicate the POST.
    http.expectNone(fuelUrl);
    request.flush(record, { status: 201, statusText: 'Created' });
    http.expectOne(`${fuelUrl}?page=1&pageSize=20`).flush({ items: [record], page: 1, pageSize: 20, totalCount: 1 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('was created');
    expect(component['formOpen']()).toBe(false);
  });

  it('requires a selected vehicle and valid form even when submission is called directly', () => {
    const component = start();
    component['beginCreate']();
    http.expectOne(`${base}/vehicles?page=1&pageSize=20`).flush({ items: [vehicle], page: 1, pageSize: 20, totalCount: 1 });
    component['createRecord']();
    http.expectNone(fuelUrl);
    expect(component['formError']()).toContain('correct');
  });

  it.each([
    ['quantityLiters', 0], ['quantityLiters', 1.2345], ['quantityLiters', Infinity],
    ['unitPrice', -1], ['unitPrice', 10.123], ['odometer', 47999], ['odometer', 48000.12],
    ['occurredAt', 'invalid'], ['occurredAt', '2026-02-30T09:00'], ['occurredAt', ''],
    ['currencyCode', 'ET'], ['station', 'x'.repeat(161)], ['receiptReference', 'x'.repeat(101)], ['notes', 'x'.repeat(1001)],
  ])('blocks invalid %s value %s without a POST', (name, value) => {
    const component = start();
    chooseCreateVehicle(component);
    validForm(component);
    component['form'].patchValue({ [name]: value });
    component['createRecord']();
    expect(component['form'].invalid).toBe(true);
    http.expectNone(fuelUrl);
  });

  it('keeps create input on server validation failure and can retry', () => {
    const component = start();
    chooseCreateVehicle(component);
    validForm(component);
    component['createRecord']();
    http.expectOne(fuelUrl).flush({ errors: { Odometer: ['Mileage changed. Reload the vehicle.'] } }, { status: 400, statusText: 'Bad Request' });
    expect(component['formOpen']()).toBe(true);
    expect(component['saving']()).toBe(false);
    expect(component['formError']()).toContain('Mileage changed');
    expect(component['form'].controls.quantityLiters.value).toBe(12.345);
    component['createRecord']();
    http.expectOne(fuelUrl).flush({ error: 'Vehicle was not found.' }, { status: 404, statusText: 'Not Found' });
    expect(component['formError']()).toBe('Vehicle was not found.');
  });

  it('paginates vehicles without excluding non-active vehicles or losing the selection', () => {
    const component = start();
    component['beginCreate']();
    http.expectOne(`${base}/vehicles?page=1&pageSize=20`).flush({ items: [], page: 1, pageSize: 20, totalCount: 21 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Page 1 of 2');
    component['goToVehiclePage'](2);
    const inactive = { ...vehicle, status: VehicleStatus.Inactive };
    http.expectOne(`${base}/vehicles?page=2&pageSize=20`).flush({ items: [inactive], page: 2, pageSize: 20, totalCount: 21 });
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.fuel-vehicle-choices button') as HTMLButtonElement).click();
    expect(component['form'].controls.vehicleId.value).toBe(vehicle.id);
    component['openPicker']('create');
    http.expectOne(`${base}/vehicles?page=1&pageSize=20`).flush({ items: [], page: 1, pageSize: 20, totalCount: 21 });
    expect(component['createVehicle']()?.id).toBe(vehicle.id);
  });

  it('retries failed vehicle pages and searches without truncating the result set', () => {
    const component = start();
    component['openPicker']('filter');
    http.expectOne(`${base}/vehicles?page=1&pageSize=20`).flush(null, { status: 500, statusText: 'Error' });
    expect(component['vehicleError']()).toContain('could not be loaded');
    component['loadVehicles']();
    http.expectOne(`${base}/vehicles?page=1&pageSize=20`).flush({ items: [], page: 1, pageSize: 20, totalCount: 41 });
    component['goToVehiclePage'](3);
    http.expectOne(`${base}/vehicles?page=3&pageSize=20`).flush(null, { status: 500, statusText: 'Error' });
    component['loadVehicles']();
    http.expectOne(`${base}/vehicles?page=3&pageSize=20`).flush({ items: [vehicle], page: 3, pageSize: 20, totalCount: 41 });
    component['vehicleSearch'].setValue(' AA ');
    component['loadVehicles'](1);
    http.expectOne(`${base}/vehicles?page=1&pageSize=20&search=AA`).flush({ items: [vehicle], page: 1, pageSize: 20, totalCount: 1 });
  });

  it('supports FleetManager vehicle filtering, resets fuel pages and clears the filter', () => {
    role.set('FleetManager');
    const component = start([record], 21);
    component['goToPage'](2);
    http.expectOne(`${fuelUrl}?page=2&pageSize=20`).flush({ items: [record], page: 2, pageSize: 20, totalCount: 21 });
    component['openPicker']('filter');
    http.expectOne(`${base}/vehicles?page=1&pageSize=20`).flush({ items: [vehicle], page: 1, pageSize: 20, totalCount: 1 });
    component['selectVehicle'](vehicle);
    http.expectOne(`${fuelUrl}?page=1&pageSize=20&vehicleId=vehicle-1`).flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
    expect(component['page']()).toBe(1);
    component['clearFilter']();
    http.expectOne(`${fuelUrl}?page=1&pageSize=20`).flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
  });

  it('retries the requested fuel page after failure, handles forbidden, and prevents out-of-range paging', () => {
    const component = start([record], 21);
    component['goToPage'](0);
    component['goToPage'](3);
    http.expectNone(() => true);
    component['goToPage'](2);
    http.expectOne(`${fuelUrl}?page=2&pageSize=20`).flush(null, { status: 403, statusText: 'Forbidden' });
    expect(component['errorMessage']()).toContain('permission');
    component['loadRecords']();
    http.expectOne(`${fuelUrl}?page=2&pageSize=20`).flush({ items: [record], page: 2, pageSize: 20, totalCount: 21 });
    expect(component['page']()).toBe(2);
  });

  it('requires confirmation, deletes, and moves back from an emptied last page', () => {
    const component = start([record], 21);
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);
    component['deleteRecord'](record);
    http.expectNone(`${fuelUrl}/fuel-1`);
    component['goToPage'](2);
    http.expectOne(`${fuelUrl}?page=2&pageSize=20`).flush({ items: [record], page: 2, pageSize: 20, totalCount: 21 });
    confirm.mockReturnValue(true);
    component['deleteRecord'](record);
    const request = http.expectOne(`${fuelUrl}/fuel-1`);
    expect(request.request.method).toBe('DELETE');
    component['deleteRecord'](record);
    http.expectNone(`${fuelUrl}/fuel-1`);
    request.flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne(`${fuelUrl}?page=1&pageSize=20`).flush({ items: [], page: 1, pageSize: 20, totalCount: 20 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Fuel record deleted.');
  });

  it('preserves rows and reports server delete failures, then allows another attempt', () => {
    const component = start([record]);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    component['deleteRecord'](record);
    http.expectOne(`${fuelUrl}/fuel-1`).flush({ error: 'Record no longer exists.' }, { status: 404, statusText: 'Not Found' });
    expect(component['deleting']()).toBeNull();
    expect(component['records']()?.items).toHaveLength(1);
    expect(component['errorMessage']()).toBe('Record no longer exists.');
    component['deleteRecord'](record);
    http.expectOne(`${fuelUrl}/fuel-1`).flush(null, { status: 403, statusText: 'Forbidden' });
    expect(component['errorMessage']()).toContain('permission');
  });

  it('displays each record currency without summing mixed costs', () => {
    start([record, { ...record, id: 'fuel-2', currencyCode: 'USD', totalCost: 25 }]);
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('ETB 1,237.59');
    expect(text).toContain('USD 25.00');
    expect(text).not.toContain('1,262.59');
  });

  it('formats a local datetime without treating local wall time as UTC', () => {
    const date = new Date('2026-11-13T06:00:00Z');
    const pad = (value: number) => String(value).padStart(2, '0');
    expect(localDateTimeInput(date)).toBe(`${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`);
  });
});
