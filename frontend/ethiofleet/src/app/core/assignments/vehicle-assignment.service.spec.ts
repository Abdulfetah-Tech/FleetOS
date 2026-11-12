import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../api/api-config';
import { VehicleAssignmentService } from './vehicle-assignment.service';

describe('VehicleAssignmentService', () => {
  let service: VehicleAssignmentService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.test/api/v1' },
      ],
    });
    service = TestBed.inject(VehicleAssignmentService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses the vehicle-scoped assignment endpoint for reads and mutations', () => {
    service.getActive('vehicle/1').subscribe();
    const read = http.expectOne('http://api.test/api/v1/vehicles/vehicle%2F1/assignment');
    expect(read.request.method).toBe('GET');
    read.flush(assignmentResponse());

    service.assign('vehicle/1', {
      vehicleId: 'vehicle/1',
      driverId: 'driver-1',
      notes: 'Morning route',
    }).subscribe();
    const create = http.expectOne('http://api.test/api/v1/vehicles/vehicle%2F1/assignment');
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual({
      vehicleId: 'vehicle/1',
      driverId: 'driver-1',
      notes: 'Morning route',
    });
    create.flush(assignmentResponse());

    service.unassign('vehicle/1').subscribe();
    const remove = http.expectOne('http://api.test/api/v1/vehicles/vehicle%2F1/assignment');
    expect(remove.request.method).toBe('DELETE');
    remove.flush({ ...assignmentResponse(), isActive: false, unassignedAt: '2026-03-01T10:00:00Z' });
  });
});

function assignmentResponse() {
  return {
    id: 'assignment-1',
    organizationId: 'organization-1',
    vehicleId: 'vehicle/1',
    driverId: 'driver-1',
    vehiclePlateNumber: 'ET-0148',
    driverFullName: 'Aster Bekele',
    assignedAt: '2026-03-01T08:00:00Z',
    unassignedAt: null,
    isActive: true,
    notes: 'Morning route',
  };
}
