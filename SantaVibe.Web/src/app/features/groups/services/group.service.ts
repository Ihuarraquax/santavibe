import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom, Observable, of } from 'rxjs';
import { GroupsService } from '@api/api/groups.service';
import { GroupDto } from '@api/model/group-dto';
import { GetUserGroupsResponse } from '@api/model/get-user-groups-response';
import {
  GroupDetailsViewModel,
  ParticipantViewModel,
  DrawValidationViewModel,
  ExclusionRuleViewModel
} from '../models/group-details.viewmodel';
import {
  GetGroupDetailsResponse,
  ParticipantDto,
  DrawValidationDto
} from '@api/model/models';

/**
 * Service for managing groups state and API interactions.
 * Uses Angular Signals for reactive state management.
 */
@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private groupsService = inject(GroupsService);

  // State signals
  private groupsSignal = signal<GroupDto[]>([]);
  activeGroup = signal<GroupDetailsViewModel | null>(null);

  // Readonly accessor
  readonly groups = this.groupsSignal.asReadonly();

  /**
   * Fetch all groups for the authenticated user.
   * Updates the groups signal with the response.
   * @param includeCompleted - Whether to include groups where draw is completed (default: true)
   * @returns Promise of GetUserGroupsResponse
   */
  async fetchGroups(includeCompleted: boolean = true): Promise<GetUserGroupsResponse> {
    const response = await firstValueFrom(
      this.groupsService.getUserGroups({ includeCompleted })
    );

    // Update signal with groups (handle null case)
    this.groupsSignal.set(response.groups ?? []);
    return response;
  }

  /**
   * Clear groups state (used on logout).
   */
  clearGroups(): void {
    this.groupsSignal.set([]);
  }

  // ===== Group Details Methods =====

  /**
   * Fetches group details from the API.
   */
  fetchGroupDetails(groupId: string): Observable<GetGroupDetailsResponse> {
    return this.groupsService.getGroupDetails({ groupId });
  }

  /**
   * Fetches the current user's wishlist for a group.
   * TODO: Implement when API endpoint is ready
   */
  fetchMyWishlist(groupId: string): Observable<string | null> {
    return of(null);
  }

  /**
   * Updates the current user's wishlist.
   * TODO: Implement when API endpoint is ready
   */
  updateWishlist(groupId: string, content: string): Observable<void> {
    return of(undefined);
  }

  /**
   * Fetches the current user's budget suggestion.
   * TODO: Implement when API endpoint is ready
   */
  fetchMyBudgetSuggestion(groupId: string): Observable<number | null> {
    return of(null);
  }

  /**
   * Updates the current user's budget suggestion.
   * TODO: Implement when API endpoint is ready
   */
  updateBudgetSuggestion(groupId: string, amount: number | null): Observable<void> {
    return of(undefined);
  }

  /**
   * Fetches anonymous budget suggestions (organizer only).
   * TODO: Implement when API endpoint is ready
   */
  fetchBudgetSuggestions(groupId: string): Observable<number[]> {
    return of([]);
  }

  /**
   * Fetches exclusion rules (organizer only).
   * TODO: Implement when API endpoint is ready
   */
  fetchExclusionRules(groupId: string): Observable<ExclusionRuleViewModel[]> {
    return of([]);
  }

  /**
   * Creates a new exclusion rule (organizer only).
   * TODO: Implement when API endpoint is ready
   */
  createExclusionRule(groupId: string, user1Id: string, user2Id: string): Observable<void> {
    return of(undefined);
  }

  /**
   * Deletes an exclusion rule (organizer only).
   * TODO: Implement when API endpoint is ready
   */
  deleteExclusionRule(groupId: string, ruleId: string): Observable<void> {
    return of(undefined);
  }

  /**
   * Removes a participant from the group (organizer only).
   * TODO: Implement when API endpoint is ready
   */
  removeParticipant(groupId: string, userId: string): Observable<void> {
    return of(undefined);
  }

  /**
   * Executes the Secret Santa draw (organizer only).
   * TODO: Implement when API endpoint is ready
   */
  executeDraw(groupId: string, budget: number): Observable<void> {
    return of(undefined);
  }

  /**
   * Fetches the recipient's wishlist (post-draw).
   * TODO: Implement when API endpoint is ready
   */
  fetchRecipientWishlist(groupId: string): Observable<string | null> {
    return of(null);
  }

  /**
   * Generates AI gift suggestions based on recipient's wishlist (post-draw).
   * TODO: Implement when API endpoint is ready
   */
  generateGiftSuggestions(groupId: string): Observable<any[]> {
    return of([]);
  }

  // ===== Mapping Methods =====

  /**
   * Maps API DTO to GroupDetailsViewModel.
   */
  mapToViewModel(dto: GetGroupDetailsResponse): GroupDetailsViewModel {
    const base = {
      groupId: dto.groupId,
      name: dto.name || '',
      organizerName: dto.organizerName || '',
      isOrganizer: dto.isOrganizer,
      participantCount: dto.participantCount,
      createdAt: new Date(dto.createdAt),
      drawCompleted: dto.drawCompleted,
      budget: dto.budget ?? null,
      drawCompletedAt: dto.drawCompletedAt ? new Date(dto.drawCompletedAt) : null
    };

    if (!dto.drawCompleted) {
      // Pre-draw
      return {
        ...base,
        participants: dto.participants?.map(p => this.mapParticipant(p)) || [],
        exclusionRuleCount: dto.exclusionRuleCount ?? 0,
        invitationLink: dto.invitationLink ?? undefined,
        canDraw: dto.canDraw ?? false,
        drawValidation: dto.drawValidation ? this.mapDrawValidation(dto.drawValidation) : {
          isValid: false,
          errors: [],
          hasErrors: false
        }
      };
    } else {
      // Post-draw
      return {
        ...base,
        myAssignment: dto.myAssignment ? {
          recipientId: dto.myAssignment.recipientId || '',
          recipientFirstName: dto.myAssignment.recipientFirstName || '',
          recipientLastName: dto.myAssignment.recipientLastName || '',
          recipientFullName: `${dto.myAssignment.recipientFirstName || ''} ${dto.myAssignment.recipientLastName || ''}`.trim(),
          hasWishlist: dto.myAssignment.hasWishlist,
          budget: dto.budget || 0
        } : undefined
      };
    }
  }

  /**
   * Maps ParticipantDto to ParticipantViewModel.
   */
  private mapParticipant(dto: ParticipantDto): ParticipantViewModel {
    return {
      userId: dto.userId || '',
      firstName: dto.firstName || '',
      lastName: dto.lastName || '',
      fullName: `${dto.firstName || ''} ${dto.lastName || ''}`.trim(),
      joinedAt: new Date(dto.joinedAt),
      hasWishlist: dto.hasWishlist,
      isOrganizer: dto.isOrganizer,
      canRemove: !dto.isOrganizer
    };
  }

  /**
   * Maps DrawValidationDto to DrawValidationViewModel.
   */
  private mapDrawValidation(dto: DrawValidationDto): DrawValidationViewModel {
    const errors = dto.errors || [];
    return {
      isValid: dto.isValid,
      errors,
      hasErrors: errors.length > 0
    };
  }
}

