import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { AuthenticationService } from '@api/api/authentication.service';
import { RegisterRequest } from '@api/model/register-request';
import { RegisterResponse } from '@api/model/register-response';
import { LoginRequest } from '@api/model/login-request';
import { LoginResponse } from '@api/model/login-response';
import { ErrorResponse } from '@api/model/error-response';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthUser } from '../models/login.types';

/**
 * Authentication service that handles user registration, login, and session management.
 * Wraps the generated API client and provides additional functionality.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private authenticationService = inject(AuthenticationService);
  private router = inject(Router);

  /**
   * Signal that tracks whether a user is currently logged in.
   */
  private isAuthenticatedSignal = signal<boolean>(false);

  /**
   * Signal that stores the current user's information.
   */
  private currentUserSignal = signal<AuthUser | null>(null);

  /**
   * Public readonly access to authentication state.
   */
  readonly isAuthenticated = this.isAuthenticatedSignal.asReadonly();

  /**
   * Public readonly access to current user information.
   */
  readonly currentUser = this.currentUserSignal.asReadonly();

  /**
   * Computed signal that checks if the current token is expired.
   */
  readonly isTokenExpired = computed(() => {
    const user = this.currentUser();
    if (!user || !user.expiresAt) return true;
    return new Date() >= user.expiresAt;
  });

  constructor() {
    // Check for existing token on service initialization
    this.loadStoredToken();
  }

  /**
   * Registers a new user account.
   * @param request - Registration request data
   * @returns Observable of registration response
   */
  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.authenticationService.register({ registerRequest: request }).pipe(
      tap((response: RegisterResponse) => {
        // Store token and user information
        if (response.token) {
          this.storeToken(response.token, response.expiresAt);
          const authUser = this.mapToAuthUser(response);
          this.currentUserSignal.set(authUser);
          this.isAuthenticatedSignal.set(true);
          localStorage.setItem('current_user', JSON.stringify(authUser));
        }
      }),
      catchError((error: HttpErrorResponse) => {
        return throwError(() => this.handleApiError(error));
      })
    );
  }

  /**
   * Logs in an existing user.
   * @param request - Login request data
   * @returns Observable of login response
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.authenticationService.login({ loginRequest: request }).pipe(
      tap((response: LoginResponse) => {
        // Store token and user information
        if (response.token && response.userId && response.email &&
            response.firstName && response.lastName) {
          this.storeToken(response.token, response.expiresAt);
          const authUser: AuthUser = {
            userId: response.userId,
            email: response.email,
            firstName: response.firstName,
            lastName: response.lastName,
            token: response.token,
            expiresAt: response.expiresAt ? new Date(response.expiresAt) : new Date()
          };
          this.currentUserSignal.set(authUser);
          this.isAuthenticatedSignal.set(true);
          localStorage.setItem('current_user', JSON.stringify(authUser));
        }
      }),
      catchError((error: HttpErrorResponse) => {
        return throwError(() => new Error(this.handleLoginError(error)));
      })
    );
  }

  /**
   * Logs out the current user.
   * Clears stored token and user information.
   */
  logout(): void {
    this.clearStoredToken();
    this.currentUserSignal.set(null);
    this.isAuthenticatedSignal.set(false);
    this.router.navigate(['/login']);
  }

  /**
   * Stores JWT token in localStorage.
   * @param token - JWT token
   * @param expiresAt - Token expiration date (optional)
   */
  private storeToken(token: string, expiresAt?: string): void {
    localStorage.setItem('auth_token', token);
    if (expiresAt) {
      localStorage.setItem('auth_token_expires', expiresAt);
    }
  }

  /**
   * Retrieves stored token from localStorage.
   * @returns JWT token or null if not found or expired
   */
  getToken(): string | null {
    const token = localStorage.getItem('auth_token');
    const expiresAt = localStorage.getItem('auth_token_expires');

    if (!token) {
      return null;
    }

    // Check if token is expired
    if (expiresAt) {
      const expirationDate = new Date(expiresAt);
      const now = new Date();
      if (now >= expirationDate) {
        this.clearStoredToken();
        return null;
      }
    }

    return token;
  }

  /**
   * Clears stored token from localStorage.
   */
  private clearStoredToken(): void {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_token_expires');
    localStorage.removeItem('current_user');
  }

  /**
   * Loads stored token on service initialization.
   * Updates authentication state if valid token exists.
   */
  private loadStoredToken(): void {
    const token = this.getToken();
    const storedUser = localStorage.getItem('current_user');

    if (token && storedUser) {
      try {
        const user = JSON.parse(storedUser) as AuthUser;
        // Convert expiresAt string back to Date object
        if (user.expiresAt) {
          user.expiresAt = new Date(user.expiresAt);
        }
        this.currentUserSignal.set(user);
        this.isAuthenticatedSignal.set(true);
      } catch {
        // Invalid stored user data, clear everything
        this.clearStoredToken();
      }
    }
  }

  /**
   * Handles API error responses and transforms them into a standardized format.
   * @param error - HTTP error response
   * @returns Standardized error response
   */
  private handleApiError(error: HttpErrorResponse): ErrorResponse {
    if (error.error && typeof error.error === 'object') {
      // Backend returned an ErrorResponse
      return error.error as ErrorResponse;
    }

    // Create a generic error response
    return {
      error: 'UnknownError',
      message: error.message || 'Wystąpił nieoczekiwany błąd',
      details: null
    };
  }

  /**
   * Maps API error details to field-specific validation errors.
   * @param errorResponse - Error response from API
   * @returns Record of field names to error message arrays
   */
  mapApiErrorsToFormErrors(errorResponse: ErrorResponse): Record<string, string[]> {
    const formErrors: Record<string, string[]> = {};

    if (errorResponse.details) {
      // Map backend field names to frontend field names (if needed)
      Object.keys(errorResponse.details).forEach(fieldName => {
        const errors = errorResponse.details![fieldName];
        // Convert to lowercase for consistent field name matching
        const normalizedFieldName = fieldName.charAt(0).toLowerCase() + fieldName.slice(1);
        formErrors[normalizedFieldName] = errors;
      });
    }

    return formErrors;
  }

  /**
   * Checks if the current user is authenticated.
   * @returns True if user is authenticated
   */
  isUserAuthenticated(): boolean {
    return this.isAuthenticatedSignal() && this.getToken() !== null;
  }

  /**
   * Handles login-specific API errors and returns user-friendly messages in Polish.
   * @param error - HTTP error response
   * @returns User-friendly error message
   */
  private handleLoginError(error: HttpErrorResponse): string {
    if (error.status === 401) {
      return 'Nieprawidłowy email lub hasło';
    } else if (error.status === 429) {
      return 'Zbyt wiele prób logowania. Spróbuj ponownie później.';
    } else if (error.status === 0) {
      return 'Błąd połączenia. Sprawdź swoje połączenie internetowe.';
    } else {
      return 'Wystąpił błąd. Spróbuj ponownie później.';
    }
  }

  /**
   * Maps registration response to AuthUser model.
   * @param response - Registration response from API
   * @returns AuthUser object
   */
  private mapToAuthUser(response: RegisterResponse): AuthUser {
    return {
      userId: response.userId || '',
      email: response.email || '',
      firstName: response.firstName || '',
      lastName: response.lastName || '',
      token: response.token || '',
      expiresAt: response.expiresAt ? new Date(response.expiresAt) : new Date()
    };
  }
}
