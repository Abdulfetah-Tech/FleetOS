import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { FleetDashboardComponent } from './features/dashboard/fleet-dashboard.component';
import { LoginComponent } from './features/auth/login.component';
import { RegistrationComponent } from './features/auth/registration.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'register', component: RegistrationComponent, canActivate: [guestGuard] },
  { path: 'dashboard', component: FleetDashboardComponent, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
