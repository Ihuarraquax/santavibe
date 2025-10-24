import {
  Component,
  ChangeDetectionStrategy,
  OnInit,
  signal,
  computed,
  inject,
  DestroyRef
} from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastrService } from 'ngx-toastr';
import { InvitationService } from '../../services/invitation.service';
import { AuthService } from '../../../auth/services/auth.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { InvitationDetailsCardComponent } from '../../components/invitation-details-card/invitation-details-card.component';
import { UnauthenticatedActionsComponent } from '../../components/unauthenticated-actions/unauthenticated-actions.component';
import { JoinGroupFormComponent } from '../../components/join-group-form/join-group-form.component';
import {
  InvitationViewState,
  InvitationError,
  JoinGroupFormData
} from '../../models/invitation.types';

/**
 * Main container component that manages the invitation view state,
 * orchestrates API calls, handles authentication state detection,
 * and conditionally renders child components based on loading, error, and authentication states.
 */
@Component({
  selector: 'app-invitation',
  templateUrl: './invitation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LoadingSpinnerComponent,
    InvitationDetailsCardComponent,
    UnauthenticatedActionsComponent,
    JoinGroupFormComponent,
    RouterLink
  ]
})
export class InvitationComponent implements OnInit {
  private invitationService = inject(InvitationService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toastr = inject(ToastrService);
  private destroyRef = inject(DestroyRef);

  /**
   * Component state signal
   */
  private readonly state = signal<InvitationViewState>({
    loading: true,
    invitation: null,
    error: null,
    isJoining: false
  });

  /**
   * Computed authentication state from service
   */
  readonly isAuthenticated = computed(() => this.authService.isAuthenticated());

  /**
   * Expose read-only state to template
   */
  readonly invitationState = this.state.asReadonly();

  ngOnInit(): void {
    const token = this.route.snapshot.params['token'];

    // Check if user just returned from auth flow
    const storedToken = this.invitationService.getStoredInvitationToken();
    if (storedToken === token && this.isAuthenticated()) {
      // User just authenticated, clear stored token
      this.invitationService.clearStoredInvitationToken();
    }

    // Load invitation details
    this.loadInvitation(token);
  }

  /**
   * Loads invitation details from API
   */
  private loadInvitation(token: string): void {
    this.state.update(s => ({ ...s, loading: true, error: null }));

    this.invitationService.getInvitationDetails(token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invitation) => {
          this.state.update(s => ({
            ...s,
            loading: false,
            invitation,
            error: null
          }));
        },
        error: (httpError: HttpErrorResponse) => {
          const mappedError = this.mapError(httpError);
          this.state.update(s => ({
            ...s,
            loading: false,
            invitation: null,
            error: mappedError
          }));
        }
      });
  }

  /**
   * Handles navigation to login page
   */
  navigateToLogin(): void {
    const token = this.route.snapshot.params['token'];
    this.invitationService.storeInvitationToken(token);
    this.router.navigate(['/login'], {
      queryParams: { returnUrl: `/invite/${token}` }
    });
  }

  /**
   * Handles navigation to registration page
   */
  navigateToRegister(): void {
    const token = this.route.snapshot.params['token'];
    this.invitationService.storeInvitationToken(token);
    this.router.navigate(['/register'], {
      queryParams: { returnUrl: `/invite/${token}` }
    });
  }

  /**
   * Handles join group form submission
   */
  handleJoinGroup(formData: JoinGroupFormData): void {
    const token = this.state().invitation!.invitationToken!;

    this.state.update(s => ({ ...s, isJoining: true }));

    this.invitationService.acceptInvitation(token, formData.budgetSuggestion)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.toastr.success('Pomyślnie dołączono do grupy!', 'Sukces');
          this.invitationService.clearStoredInvitationToken();
          this.router.navigate(['/groups', response.groupId]);
        },
        error: (httpError: HttpErrorResponse) => {
          const mappedError = this.mapError(httpError);

          // Special handling for already-joined
          if (mappedError.type === 'already-joined') {
            this.toastr.info(mappedError.message, 'Informacja');
            const groupId = this.state().invitation!.groupId;
            this.router.navigate(['/groups', groupId]);
          } else {
            // Show error in UI
            this.state.update(s => ({
              ...s,
              isJoining: false,
              error: mappedError
            }));
          }
        }
      });
  }

  /**
   * Handles retry after error
   */
  handleRetry(): void {
    const token = this.route.snapshot.params['token'];
    this.loadInvitation(token);
  }

  /**
   * Maps HTTP error to InvitationError
   */
  private mapError(error: HttpErrorResponse): InvitationError {
    // Network/connection errors
    if (error.status === 0) {
      return {
        type: 'network',
        message: 'Nie można połączyć się z serwerem. Sprawdź połączenie internetowe.'
      };
    }

    // Map HTTP status codes to error types
    switch (error.status) {
      case 404:
        return {
          type: 'invalid',
          message: 'To zaproszenie jest nieprawidłowe lub wygasło.'
        };

      case 409:
        return {
          type: 'already-joined',
          message: 'Jesteś już członkiem tej grupy.'
        };

      case 410:
        return {
          type: 'expired',
          message: 'Ta grupa zakończyła już losowanie i nie przyjmuje nowych uczestników.'
        };

      case 401:
        return {
          type: 'unknown',
          message: 'Twoja sesja wygasła. Zaloguj się ponownie.'
        };

      case 400:
        return {
          type: 'unknown',
          message: error.error?.message || 'Nieprawidłowe dane. Sprawdź formularz.'
        };

      default:
        return {
          type: 'unknown',
          message: 'Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.'
        };
    }
  }

  /**
   * Returns error title based on error type
   */
  getErrorTitle(errorType: string): string {
    switch (errorType) {
      case 'invalid':
        return 'Nieprawidłowe zaproszenie';
      case 'expired':
        return 'Zaproszenie wygasło';
      case 'already-joined':
        return 'Już jesteś w grupie';
      case 'network':
        return 'Błąd połączenia';
      default:
        return 'Wystąpił błąd';
    }
  }
}
