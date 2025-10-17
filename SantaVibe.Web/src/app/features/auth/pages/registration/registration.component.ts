import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RegistrationFormComponent } from '../../components/registration-form/registration-form.component';

/**
 * Registration page component that serves as the route-level wrapper.
 * Provides layout and branding for the registration view.
 */
@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  styleUrls: ['./registration.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RegistrationFormComponent]
})
export class RegistrationComponent {}
