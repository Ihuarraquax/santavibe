import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/register',
    pathMatch: 'full'
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/pages/registration/registration.component').then(m => m.RegistrationComponent),
    title: 'Rejestracja - SantaVibe'
  },
  {
    path: '**',
    redirectTo: '/register'
  }
];
