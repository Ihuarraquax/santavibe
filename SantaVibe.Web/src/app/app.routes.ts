import { Routes } from '@angular/router';
import { guestGuard } from './features/auth/guards/guest.guard';
import { authGuard } from './features/auth/guards/auth.guard';
import { unsavedChangesGuard } from './core/guards/unsaved-changes.guard';

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
    path: 'groups/create',
    loadComponent: () => import('./features/groups/pages/group-create/group-create.component').then(m => m.GroupCreateComponent),
    canActivate: [authGuard],
    canDeactivate: [unsavedChangesGuard],
    title: 'Utwórz Grupę - SantaVibe'
  },
  {
    path: 'invite/:token',
    loadComponent: () => import('./features/invitations/pages/invitation/invitation.component').then(m => m.InvitationComponent),
    title: 'Zaproszenie do grupy - SantaVibe'
  },
  {
    path: '**',
    redirectTo: '/groups'
  }
];
