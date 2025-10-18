import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../../features/auth/services/auth.service';

/**
 * HTTP interceptor that handles global error responses.
 * Specifically handles 401 Unauthorized errors by logging out the user and redirecting to login.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle 401 Unauthorized errors
      if (error.status === 401) {
        // Clear authentication state
        authService.logout();

        // Redirect to login page with return URL
        router.navigate(['/login'], {
          queryParams: { returnUrl: router.url }
        });
      }

      // Re-throw the error so components can handle it
      return throwError(() => error);
    })
  );
};
