import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../api/api-config';
import { CreateDriverRequest, DriverStatus } from './driver.models';
import { DriverService } from './driver.service';

describe('DriverService', () => {
  let http: HttpTestingController;
  let service: DriverService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        DriverService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.test/api/v1' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(DriverService);
  });

  afterEach(() => http.verify());

  it('trims search text and sends paginated driver queries', () => {
    service.list(2, 10, '  hana  ').subscribe();

    const request = http.expectOne('http://api.test/api/v1/drivers?page=2&pageSize=10&search=hana');
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], page: 2, pageSize: 10, totalCount: 0 });
  });

  it('sends create, update, status, and delete requests to the driver API', () => {
    const payload: CreateDriverRequest = {
      fullName: 'Hana Bekele',
      email: 'hana@example.test',
      phoneNumber: '+251911123456',
      licenseNumber: 'AB-1234',
      licenseCategory: 'B',
      licenseExpiryDate: '2027-01-01',
      emergencyContactName: null,
      emergencyContactPhone: null,
    };

    service.create(payload).subscribe();
    const create = http.expectOne('http://api.test/api/v1/drivers');
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual(payload);
    create.flush(driverResponse(DriverStatus.Active));

    service.update('driver-1', payload).subscribe();
    const update = http.expectOne('http://api.test/api/v1/drivers/driver-1');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual(payload);
    update.flush(driverResponse(DriverStatus.Active));

    service.changeStatus('driver-1', DriverStatus.Suspended).subscribe();
    const status = http.expectOne('http://api.test/api/v1/drivers/driver-1/status');
    expect(status.request.method).toBe('PATCH');
    expect(status.request.body).toEqual({ status: DriverStatus.Suspended });
    status.flush(driverResponse(DriverStatus.Suspended));

    service.delete('driver-1').subscribe();
    const remove = http.expectOne('http://api.test/api/v1/drivers/driver-1');
    expect(remove.request.method).toBe('DELETE');
    remove.flush(null);
  });
});

function driverResponse(status: DriverStatus) {
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
    licenseExpired: false,
    status,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}
