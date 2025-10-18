import { Routes } from '@angular/router';
import { guestGuard } from './features/auth/guards/guest.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/login',
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
    path: '**',
    redirectTo: '/login'
  }
];
