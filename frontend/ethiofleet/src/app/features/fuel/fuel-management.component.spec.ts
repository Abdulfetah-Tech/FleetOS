
// Minimal Jasmine-like test helpers for this spec
const jasmine = {
  createSpy: (name: string) => {
    const fn: any = (...args: any[]) => {
      fn.calls.push(args);
      return fn.and?.returnValue;
    };
    fn.calls = [] as any[];
    fn.and = {
      returnValue: (val: any) => {
        fn.and.returnValue = val;
        return fn;
      },
    };
    return fn;
  },
};

function spyOn(obj: any, method: string) {
  const original = obj[method];
  const spy = jasmine.createSpy(method);
  obj[method] = spy;
  return {
    and: {
      returnValue: (val: any) => {
        spy.and.returnValue = val;
        return spy;
      },
      callThrough: () => {
        obj[method] = original;
        return spy;
      },
    },
  };
}

function expect(actual: any) {
  return {
    toHaveBeenCalled: () => {
      if (!actual.calls || actual.calls.length === 0) {
        throw new Error('Expected spy to have been called');
      }
    },
    toHaveBeenCalledWith: (...expected: any[]) => {
      const called = actual.calls?.some((c: any) => JSON.stringify(c) === JSON.stringify(expected));
      if (!called) {
        throw new Error(`Expected spy to have been called with ${JSON.stringify(expected)}`);
      }
    },
    toBeTruthy: () => {
      if (!actual) {
        throw new Error('Expected value to be truthy');
      }
    },
    toBe: (value: any) => {
      if (actual !== value) {
        throw new Error(`Expected ${actual} to be ${value}`);
      }
    },
  };
}


import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FuelManagementComponent } from './fuel-management.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { FuelService } from '../../core/fuel/fuel.service';
import { VehicleService } from '../../core/vehicles/vehicle.service';
import { VehicleAssignmentService } from '../../core/assignments/vehicle-assignment.service';
import { AuthService } from '../../core/auth/auth.service';
import { of } from 'rxjs';
import { Vehicle } from '../../core/vehicles/vehicle.models';

// Mock services without jasmine spies
class MockFuelService {
  list() { return of({ items: [], totalCount: 0, page: 1, pageSize: 20 }); }
  create() { return of({ id: '1', vehiclePlateNumber: 'ABC-123', quantityLiters: 10, totalCost: 850, driverName: null }); }
  delete() { return of(null); }
}

class MockVehicleService {
  list() { return of({ items: [{ id: 'veh1', plateNumber: 'ABC-123', currentMileage: 1000, organizationId: 'org1', make: 'Make', model: 'Model', year: 2020, vehicleStatus: 'Active', type: 'Truck', fuelType: 'Diesel', vin: 'VIN123', status: 'Active', purchaseDate: new Date().toISOString(), currencyCode: 'ETB', createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() } as unknown as Vehicle], totalCount: 1, page: 1, pageSize: 25 }); }
}

class MockVehicleAssignmentService {
  getActive() { return of({ driverId: null }); }
}

class MockAuthService {
  isCompanyOwner = () => true;
  isFleetManager = () => true;
}

describe('FuelManagementComponent', () => {
  let component: FuelManagementComponent;
  let fixture: ComponentFixture<FuelManagementComponent>;
  let fuelService: MockFuelService;
  let vehicleService: MockVehicleService;
  let assignmentService: MockVehicleAssignmentService;
  let authService: MockAuthService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, ReactiveFormsModule, FuelManagementComponent],
      providers: [
        { provide: FuelService, useClass: MockFuelService },
        { provide: VehicleService, useClass: MockVehicleService },
        { provide: VehicleAssignmentService, useClass: MockVehicleAssignmentService },
        { provide: AuthService, useClass: MockAuthService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FuelManagementComponent);
    component = fixture.componentInstance;
    fuelService = TestBed.inject(FuelService) as any;
    vehicleService = TestBed.inject(VehicleService) as any;
    assignmentService = TestBed.inject(VehicleAssignmentService) as any;
    authService = TestBed.inject(AuthService) as any;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load fuel records on init', () => {
    component.ngOnInit();
    expect(fuelService.list).toHaveBeenCalled();
  });

  it('should create a fuel record when form is valid', () => {
    component.beginCreate();
    // select vehicle
    (component as any).selectedVehicle.set({ id: 'veh1', plateNumber: 'ABC-123', currentMileage: 1000 } as Vehicle);
    (component as any).selectedVehicleId.set('veh1');
    // fill form
    (component as any).form.quantityLiters = 10;
    (component as any).form.unitPrice = 85;
    (component as any).form.odometer = 1100;
    component.submitFuelRecord();
    expect(assignmentService.getActive).toHaveBeenCalled();
    expect(fuelService.create).toHaveBeenCalled();
  });

  it('should delete a fuel record', () => {
    const record: any = { id: '1', vehiclePlateNumber: 'ABC-123', quantityLiters: 10, totalCost: 850 };
    spyOn(window, 'confirm').and.returnValue(true);
    component.deleteRecord(record);
    expect(fuelService.delete).toHaveBeenCalledWith('1');
  });
});
