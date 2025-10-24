import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { InvitationsService } from '@api/api/invitations.service';
import {
  InvitationDetailsDto,
  AcceptInvitationResponseDto
} from '../models/invitation.types';

/**
 * Invitation service that wraps the generated API client
 * and provides additional functionality for token persistence.
 */
@Injectable({
  providedIn: 'root'
})
export class InvitationService {
  private invitationsService = inject(InvitationsService);

  /**
   * Get invitation details by token (public endpoint)
   * @param token Invitation token (UUID)
   * @returns Observable<InvitationDetailsDto>
   */
  getInvitationDetails(token: string): Observable<InvitationDetailsDto> {
    return this.invitationsService.getInvitationDetails({ token }).pipe(
      catchError((error) => throwError(() => error))
    );
  }

  /**
   * Accept invitation and join group (requires authentication)
   * @param token Invitation token (UUID)
   * @param budgetSuggestion Optional budget suggestion in PLN
   * @returns Observable<AcceptInvitationResponseDto>
   */
  acceptInvitation(
    token: string,
    budgetSuggestion?: number | null
  ): Observable<AcceptInvitationResponseDto> {
    return this.invitationsService.acceptInvitation({
      token,
      acceptInvitationRequest: {
        budgetSuggestion: budgetSuggestion ?? null
      }
    }).pipe(
      catchError((error) => throwError(() => error))
    );
  }

  /**
   * Store invitation token for use after authentication
   */
  storeInvitationToken(token: string): void {
    sessionStorage.setItem('pendingInvitationToken', token);
  }

  /**
   * Retrieve stored invitation token
   */
  getStoredInvitationToken(): string | null {
    return sessionStorage.getItem('pendingInvitationToken');
  }

  /**
   * Clear stored invitation token
   */
  clearStoredInvitationToken(): void {
    sessionStorage.removeItem('pendingInvitationToken');
  }
}
