import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { GroupsService } from '@api/api/groups.service';
import { GroupDto } from '@api/model/group-dto';
import { GetUserGroupsResponse } from '@api/model/get-user-groups-response';

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
}
