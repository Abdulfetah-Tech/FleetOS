import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { MaintenanceType } from '../../core/maintenance/maintenance.models';
import { MaintenanceService } from '../../core/maintenance/maintenance.service';
import { VehicleService } from '../../core/vehicles/vehicle.service';
import { MaintenanceManagementComponent } from './maintenance-management.component';

describe('MaintenanceManagementComponent', () => {
  it('loads maintenance records and schedules on init', async () => {
    const maintenanceServiceStub = {
      listRecords: () =>
        of({
          items: [
            {
              id: 'rec-1',
              organizationId: 'org-1',
              vehicleId: 'veh-1',
              vehiclePlateNumber: 'AA-2-12345',
              type: MaintenanceType.Preventive,
              description: 'Oil change',
              performedOn: '2026-11-20',
              odometer: 12000,
              laborCost: 500,
              partsCost: 1500,
              totalCost: 2000,
              currencyCode: 'ETB',
              serviceProvider: 'Addis Auto',
              reference: null,
              notes: null,
            },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
        }),
      listSchedules: () =>
        of({
          items: [],
          page: 1,
          pageSize: 20,
          totalCount: 0,
        }),
    };

    const vehicleServiceStub = {
      list: () => of({ items: [], page: 1, pageSize: 50, totalCount: 0 }),
    };

    const authStub = {
      isCompanyOwner: () => true,
    };

    await TestBed.configureTestingModule({
      imports: [MaintenanceManagementComponent],
      providers: [
        { provide: MaintenanceService, useValue: maintenanceServiceStub },
        { provide: VehicleService, useValue: vehicleServiceStub },
        { provide: AuthService, useValue: authStub },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(MaintenanceManagementComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('AA-2-12345');
    expect(compiled.textContent).toContain('Oil change');
    expect(compiled.textContent).toContain('2000 ETB');
  });
});
