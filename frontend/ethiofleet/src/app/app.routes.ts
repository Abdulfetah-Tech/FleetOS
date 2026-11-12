import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { FleetWorkspaceComponent } from './core/layout/fleet-workspace.component';
import { FleetDashboardComponent } from './features/dashboard/fleet-dashboard.component';
import { LoginComponent } from './features/auth/login.component';
import { RegistrationComponent } from './features/auth/registration.component';
import { DriverManagementComponent } from './features/drivers/driver-management.component';
import { TripManagementComponent } from './features/trips/trip-management.component';
import { VehicleManagementComponent } from './features/vehicles/vehicle-management.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'register', component: RegistrationComponent, canActivate: [guestGuard] },
  {
    path: '',
    component: FleetWorkspaceComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: FleetDashboardComponent },
      { path: 'vehicles', component: VehicleManagementComponent },
      { path: 'drivers', component: DriverManagementComponent },
      { path: 'trips', component: TripManagementComponent },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
