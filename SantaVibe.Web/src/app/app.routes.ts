import { Routes } from '@angular/router';
import { CreateGroupComponent } from './components/create-group/create-group.component';

export const routes: Routes = [
  { path: '', redirectTo: '/create-group', pathMatch: 'full' },
  { path: 'create-group', component: CreateGroupComponent },
];
