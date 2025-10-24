import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { LoginFormComponent } from '../../components/login-form/login-form.component';
import { ErrorAlertComponent } from '../../../../shared/components/error-alert/error-alert.component';
import { AuthService } from '../../services/auth.service';
import { LoginFormData } from '../../models/login.types';
import { LoginRequest } from '@api/model/login-request';

/**
 * Smart container component for the login page.
 * Manages authentication flow, API calls, navigation, and error handling.
 */
@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LoginFormComponent, ErrorAlertComponent, RouterLink]
})
export class LoginComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  /**
   * Tracks form submission loading state.
   */
  isLoading = signal<boolean>(false);

  /**
   * Stores current error message (null when no error).
   */
  error = signal<string | null>(null);

  /**
   * Destination URL after successful login.
   */
  returnUrl = signal<string>('/groups');

  ngOnInit(): void {
    // Redirect if already authenticated
    if (this.authService.isAuthenticated() && !this.authService.isTokenExpired()) {
      this.router.navigate(['/groups']);
      return;
    }

    // Extract and validate return URL from query params
    const returnUrl = this.route.snapshot.queryParams['returnUrl'];
    if (returnUrl && typeof returnUrl === 'string' && returnUrl.startsWith('/')) {
      this.returnUrl.set(returnUrl);
    }
  }

  /**
   * Handles form submission from LoginFormComponent.
   * Calls AuthService, manages loading state, and handles navigation.
   */
  onFormSubmit(formData: LoginFormData): void {
    // Clear previous errors
    this.error.set(null);

    // Set loading state
    this.isLoading.set(true);

    // Prepare request (normalize email)
    const request: LoginRequest = {
      email: formData.email.toLowerCase().trim(),
      password: formData.password
    };

    // Call AuthService
    this.authService.login(request).subscribe({
      next: () => {
        // Success: Navigate to return URL or dashboard
        this.isLoading.set(false);
        this.router.navigate([this.returnUrl()]);
      },
      error: (error: Error) => {
        // Error: Display error message
        this.isLoading.set(false);
        this.error.set(error.message);
      }
    });
  }

  /**
   * Handles error alert dismissal.
   */
  onDismissError(): void {
    this.error.set(null);
  }

  /**
   * Navigates to registration page while preserving returnUrl.
   */
  onNavigateToRegister(): void {
    const returnUrl = this.returnUrl();
    this.router.navigate(['/register'], {
      queryParams: returnUrl !== '/groups' ? { returnUrl } : {}
    });
  }
}
