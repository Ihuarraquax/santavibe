import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { RegistrationFormComponent } from '../../components/registration-form/registration-form.component';

/**
 * Container component for the registration page.
 * Manages the registration page layout and orchestrates the registration flow.
 */
@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  styleUrls: ['./registration.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RegistrationFormComponent, RouterLink]
})
export class RegistrationComponent {
  private router = inject(Router);

  /**
   * Navigates to the login page.
   */
  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }
}
