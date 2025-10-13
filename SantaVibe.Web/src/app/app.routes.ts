import { Routes } from '@angular/router';
import { AddGroupComponent } from './components/add-group/add-group.component';

export const routes: Routes = [
  { path: 'add-group', component: AddGroupComponent },
  { path: '', redirectTo: '/add-group', pathMatch: 'full' }
];
