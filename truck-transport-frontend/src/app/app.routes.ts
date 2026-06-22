import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'trips',
        loadComponent: () =>
          import('./features/trips/trip-list/trip-list.component').then((m) => m.TripListComponent),
      },
      {
        path: 'trips/new',
        loadComponent: () =>
          import('./features/trips/trip-form/trip-form.component').then((m) => m.TripFormComponent),
      },
      {
        path: 'trips/:id/edit',
        loadComponent: () =>
          import('./features/trips/trip-form/trip-form.component').then((m) => m.TripFormComponent),
      },
      {
        path: 'repairs',
        loadComponent: () =>
          import('./features/repairs/repair-list/repair-list.component').then((m) => m.RepairListComponent),
      },
      {
        path: 'repairs/new',
        loadComponent: () =>
          import('./features/repairs/repair-form/repair-form.component').then((m) => m.RepairFormComponent),
      },
      {
        path: 'repairs/:id/edit',
        loadComponent: () =>
          import('./features/repairs/repair-form/repair-form.component').then((m) => m.RepairFormComponent),
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/admin.component').then((m) => m.AdminComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
