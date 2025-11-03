import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom, Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { GroupsService } from '@api/api/groups.service';
import { WishlistsService } from '@api/api/wishlists.service';
import { BudgetService } from '@api/api/budget.service';
import { ExclusionRulesService } from '@api/api/exclusion-rules.service';
import { DrawService } from '@api/api/draw.service';
import { AssignmentsService } from '@api/api/assignments.service';
import { GroupDto } from '@api/model/group-dto';
import { GetUserGroupsResponse } from '@api/model/get-user-groups-response';
import {
  GroupDetailsViewModel,
  ParticipantViewModel,
  DrawValidationViewModel,
  ExclusionRuleViewModel,
  GiftSuggestion
} from '../models/group-details.viewmodel';
import {
  GetGroupDetailsResponse,
  ParticipantDto,
  DrawValidationDto,
  ExclusionRuleDto
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
  private wishlistsService = inject(WishlistsService);
  private budgetService = inject(BudgetService);
  private exclusionRulesService = inject(ExclusionRulesService);
  private drawService = inject(DrawService);
  private assignmentsService = inject(AssignmentsService);

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
   */
  fetchMyWishlist(groupId: string): Observable<string | null> {
    return this.wishlistsService.getMyWishlist({ groupId }).pipe(
      map(response => response.wishlistContent ?? null)
    );
  }

  /**
   * Updates the current user's wishlist.
   */
  updateWishlist(groupId: string, content: string): Observable<void> {
    return this.wishlistsService.updateWishlist({
      groupId,
      updateWishlistRequest: { wishlistContent: content }
    }).pipe(
      map(() => undefined)
    );
  }

  /**
   * Fetches the current user's budget suggestion.
   */
  fetchMyBudgetSuggestion(groupId: string): Observable<number | null> {
    // The budget suggestion is returned as part of updateBudgetSuggestion response
    // For fetching, we'll need to check if there's a separate endpoint or use the group details
    // For now, returning null as a placeholder - the actual value comes from group details
    return of(null);
  }

  /**
   * Updates the current user's budget suggestion.
   */
  updateBudgetSuggestion(groupId: string, amount: number | null): Observable<void> {
    return this.budgetService.updateBudgetSuggestion({
      groupId,
      updateBudgetSuggestionRequest: { budgetSuggestion: amount }
    }).pipe(
      map(() => undefined)
    );
  }

  /**
   * Fetches anonymous budget suggestions (organizer only).
   */
  fetchBudgetSuggestions(groupId: string): Observable<number[]> {
    return this.groupsService.getBudgetSuggestions({ groupId }).pipe(
      map(response => response.suggestions ?? [])
    );
  }

  /**
   * Fetches exclusion rules (organizer only).
   */
  fetchExclusionRules(groupId: string): Observable<ExclusionRuleViewModel[]> {
    return this.exclusionRulesService.getExclusionRules({ groupId }).pipe(
      map(response => (response.exclusionRules ?? []).map((rule: ExclusionRuleDto) => this.mapExclusionRule(rule)))
    );
  }

  /**
   * Creates a new exclusion rule (organizer only).
   */
  createExclusionRule(groupId: string, user1Id: string, user2Id: string): Observable<void> {
    return this.exclusionRulesService.createExclusionRule({
      groupId,
      createExclusionRuleRequest: { userId1: user1Id, userId2: user2Id }
    }).pipe(
      map(() => undefined)
    );
  }

  /**
   * Deletes an exclusion rule (organizer only).
   */
  deleteExclusionRule(groupId: string, ruleId: string): Observable<void> {
    return this.exclusionRulesService.deleteExclusionRule({
      groupId,
      ruleId
    }).pipe(
      map(() => undefined)
    );
  }

  /**
   * Removes a participant from the group (organizer only).
   */
  removeParticipant(groupId: string, userId: string): Observable<void> {
    // Note: This endpoint might not be in the generated API yet
    // If it's missing, we'll need to add it to the OpenAPI spec
    return of(undefined); // Placeholder - implement when endpoint is available
  }

  /**
   * Executes the Secret Santa draw (organizer only).
   */
  executeDraw(groupId: string, budget: number): Observable<void> {
    return this.drawService.executeDraw({
      groupId,
      executeDrawRequest: { budget }
    }).pipe(
      map(() => undefined)
    );
  }

  /**
   * Fetches the recipient's wishlist (post-draw).
   * Note: This endpoint is not yet implemented in the backend API.
   */
  fetchRecipientWishlist(groupId: string): Observable<string | null> {
    // TODO: Implement when backend endpoint is ready
    // The endpoint should be: GET /api/groups/{groupId}/recipients/me/wishlist
    return of(null);
  }

  /**
   * Generates AI gift suggestions based on recipient's wishlist (post-draw).
   */
  generateGiftSuggestions(groupId: string): Observable<GiftSuggestion[]> {
    // Note: This endpoint might not be in the generated API yet
    // If it's missing, we'll need to add it to the OpenAPI spec
    return of([]); // Placeholder - implement when endpoint is available
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
          hasWishlist: dto.myAssignment.hasWishlist ?? false,
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
      isValid: dto.isValid ?? false,
      errors,
      hasErrors: errors.length > 0
    };
  }

  /**
   * Maps ExclusionRuleDto to ExclusionRuleViewModel.
   */
  private mapExclusionRule(dto: ExclusionRuleDto): ExclusionRuleViewModel {
    return {
      ruleId: dto.ruleId || '',
      user1Id: dto.user1?.userId || '',
      user1Name: `${dto.user1?.firstName || ''} ${dto.user1?.lastName || ''}`.trim(),
      user2Id: dto.user2?.userId || '',
      user2Name: `${dto.user2?.firstName || ''} ${dto.user2?.lastName || ''}`.trim()
    };
  }
}

