import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { API_BASE_URL } from '../../core/api/api-config';
import { FleetDashboardResponse } from '../../core/dashboard/fleet-dashboard.models';
import { FleetDashboardComponent } from './fleet-dashboard.component';

describe('FleetDashboardComponent', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    window.sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [FleetDashboardComponent],
      providers: [
        provideRouter([]),
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

  it('renders fleet totals and upcoming work from the API response', () => {
    const fixture = TestBed.createComponent(FleetDashboardComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Loading fleet data');

    http.expectOne('http://api.test/api/v1/dashboard/fleet').flush(dashboardResponse());
    fixture.detectChanges();

    const rendered = fixture.nativeElement.textContent as string;
    expect(rendered).toContain('64');
    expect(rendered).toContain('ET-0148 · Oil and filter service');
    expect(rendered).toContain('ETB 1,284,500');
    expect(rendered).toContain('4 items need attention');
  });

  it('shows an explicit recovery state when the fleet API is unavailable', () => {
    const fixture = TestBed.createComponent(FleetDashboardComponent);
    fixture.detectChanges();

    http.expectOne('http://api.test/api/v1/dashboard/fleet').flush(null, {
      status: 503,
      statusText: 'Service Unavailable',
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Fleet data is unavailable');
    expect(fixture.nativeElement.textContent).toContain('Check the API connection');
  });
});

function dashboardResponse(): FleetDashboardResponse {
  return {
    asOfDate: '2026-11-12',
    month: '2026-11-01',
    totalVehicles: 64,
    activeVehicles: 52,
    vehiclesInMaintenance: 4,
    availableVehicles: 8,
    totalDrivers: 34,
    activeDrivers: 29,
    tripsToday: 14,
    monthlyCosts: [{
      currencyCode: 'ETB',
      fuelCost: 921000,
      maintenanceCost: 363500,
      operatingCost: 1284500,
    }],
    dueMaintenance: [{
      scheduleId: 'schedule-id',
      vehicleId: 'vehicle-id',
      plateNumber: 'ET-0148',
      name: 'Oil and filter service',
      dueDate: '2026-11-10',
      dueMileage: 48000,
      currentMileage: 48320,
    }],
    upcomingDocumentExpiries: [{
      documentId: 'document-id',
      vehicleId: 'vehicle-id',
      plateNumber: 'ET-0314',
      name: 'Insurance',
      expiresOn: '2026-11-17',
      daysUntilExpiry: 5,
    }, {
      documentId: 'document-id-2',
      vehicleId: 'vehicle-id-2',
      plateNumber: 'ET-0187',
      name: 'Roadworthiness',
      expiresOn: '2026-11-24',
      daysUntilExpiry: 12,
    }, {
      documentId: 'document-id-3',
      vehicleId: 'vehicle-id-3',
      plateNumber: 'ET-0404',
      name: 'Inspection certificate',
      expiresOn: '2026-11-13',
      daysUntilExpiry: 1,
    }],
    openIncidents: 1,
  };
}
