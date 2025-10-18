import { Routes } from '@angular/router';
import { guestGuard } from './features/auth/guards/guest.guard';
import { authGuard } from './features/auth/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/groups',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/pages/login/login.component').then(m => m.LoginComponent),
    canActivate: [guestGuard],
    title: 'Logowanie - SantaVibe'
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/pages/registration/registration.component').then(m => m.RegistrationComponent),
    canActivate: [guestGuard],
    title: 'Rejestracja - SantaVibe'
  },
  {
    path: 'groups',
    loadComponent: () => import('./features/groups/pages/groups-list/groups-list.component').then(m => m.GroupsListComponent),
    canActivate: [authGuard],
    title: 'Moje Grupy - SantaVibe'
  },
  {
    path: '**',
    redirectTo: '/groups'
  }
];
