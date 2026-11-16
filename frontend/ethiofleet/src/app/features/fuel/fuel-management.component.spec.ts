declare var jasmine: any;
declare var jasmine: any;
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

// Mock services
class MockFuelService {
  list = jasmine.createSpy('list').and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 }));
  create = jasmine.createSpy('create').and.returnValue(of({ id: '1', vehiclePlateNumber: 'ABC-123', quantityLiters: 50, totalCost: 4250, driverName: null }));
  delete = jasmine.createSpy('delete').and.returnValue(of(null));
}
class MockVehicleService {
  list = jasmine.createSpy('list').and.returnValue(of({ items: [{ id: 'veh1', plateNumber: 'ABC-123', currentMileage: 1000, organizationId: 'org1', make: '', model: '', year: 2020, vehicleStatus: '' } as Vehicle], totalCount: 1, page: 1, pageSize: 25 }));
}
class MockVehicleAssignmentService {
  getActive = jasmine.createSpy('getActive').and.returnValue(of({ driverId: null }));
}
class MockAuthService {
  isCompanyOwner = jasmine.createSpy('isCompanyOwner').and.returnValue(true);
  isFleetManager = jasmine.createSpy('isFleetManager').and.returnValue(true);
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
      imports: [HttpClientTestingModule, ReactiveFormsModule],
      declarations: [FuelManagementComponent],
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
    // mock vehicle selection
    (component as any).selectedVehicle.set({ id: 'veh1', plateNumber: 'ABC-123', currentMileage: 1000 } as Vehicle);
    (component as any).selectedVehicleId.set('veh1');
    // set form fields via any cast
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

import { FuelManagementComponent } from './fuel-management.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { FuelService } from '../../core/fuel/fuel.service';
import { VehicleService } from '../../core/vehicles/vehicle.service';
import { VehicleAssignmentService } from '../../core/assignments/vehicle-assignment.service';
import { AuthService } from '../../core/auth/auth.service';
import { of, throwError } from 'rxjs';

// Mock services
class MockFuelService {
  list = jasmine.createSpy('list').and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 }));
  create = jasmine.createSpy('create').and.returnValue(of({ id: '1', vehiclePlateNumber: 'ABC-123', quantityLiters: 50, totalCost: 4250, driverName: null }));
  delete = jasmine.createSpy('delete').and.returnValue(of(null));
}
class MockVehicleService {
  list = jasmine.createSpy('list').and.returnValue(of({ items: [{ id: 'veh1', plateNumber: 'ABC-123', currentMileage: 1000 }], totalCount: 1, page: 1, pageSize: 25 }));
}
class MockVehicleAssignmentService {
  getActive = jasmine.createSpy('getActive').and.returnValue(of({ driverId: null }));
}
class MockAuthService {
  isCompanyOwner = jasmine.createSpy('isCompanyOwner').and.returnValue(true);
  isFleetManager = jasmine.createSpy('isFleetManager').and.returnValue(true);
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
      imports: [HttpClientTestingModule, ReactiveFormsModule],
      declarations: [FuelManagementComponent],
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
    component.vehiclePage.set({ items: [{ id: 'veh1', plateNumber: 'ABC-123', currentMileage: 1000 }], totalCount: 1, page: 1, pageSize: 25 });
    component.onVehicleSelected('veh1');
    component.form.quantityLiters = 10;
    component.form.unitPrice = 85;
    component.form.odometer = 1100;
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
