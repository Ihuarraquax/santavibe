import { Component, OnInit, inject, signal, computed, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, switchMap, finalize, tap, catchError, of } from 'rxjs';
import { GroupService } from '../../services/group.service';
import {
  GroupDetailsViewModel,
  ExclusionRuleViewModel,
  GiftSuggestion
} from '../../models/group-details.viewmodel';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorAlertComponent } from '../../../../shared/components/error-alert/error-alert.component';
import { GroupInfoCardComponent } from '../../components/group-info-card/group-info-card';
import { ParticipantListComponent } from '../../components/participant-list/participant-list';
import { WishlistEditorComponent } from '../../components/wishlist-editor/wishlist-editor';
import { BudgetSuggestionComponent } from '../../components/budget-suggestion/budget-suggestion';
import { InvitationLinkCard } from '../../components/invitation-link-card/invitation-link-card';

/**
 * Main container component for the Group Details view.
 * Orchestrates all data fetching, state management, and user interactions.
 * Conditionally renders child components based on user role and draw state.
 */
@Component({
  selector: 'app-group-details',
  imports: [
    LoadingSpinnerComponent,
    ErrorAlertComponent,
    GroupInfoCardComponent,
    ParticipantListComponent,
    WishlistEditorComponent,
    BudgetSuggestionComponent,
    InvitationLinkCard
  ],
  templateUrl: './group-details.html',
  styleUrl: './group-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GroupDetailsComponent implements OnInit {
  private groupService = inject(GroupService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  // Route params
  private groupId = signal<string>('');

  // Loading and error states
  isLoading = signal<boolean>(true);
  error = signal<string | null>(null);
  isRefreshing = signal<boolean>(false);

  // Group data
  groupDetails = signal<GroupDetailsViewModel | null>(null);

  // Pre-draw form states
  myWishlist = signal<string>('');
  myBudgetSuggestion = signal<number | null>(null);
  isSavingWishlist = signal<boolean>(false);
  isSavingBudget = signal<boolean>(false);

  // Organizer states (pre-draw)
  budgetSuggestions = signal<number[]>([]);
  exclusionRules = signal<ExclusionRuleViewModel[]>([]);
  isExecutingDraw = signal<boolean>(false);

  // Post-draw states
  recipientWishlist = signal<string | null>(null);
  giftSuggestions = signal<GiftSuggestion[]>([]);
  isGeneratingSuggestions = signal<boolean>(false);

  // Computed signals
  isPreDraw = computed(() => !this.groupDetails()?.drawCompleted);
  isPostDraw = computed(() => this.groupDetails()?.drawCompleted ?? false);
  isOrganizer = computed(() => this.groupDetails()?.isOrganizer ?? false);
  canExecuteDraw = computed(() =>
    this.groupDetails()?.drawValidation?.isValid ?? false
  );

  ngOnInit(): void {
    // Extract groupId from route and fetch data
    this.route.params
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        this.groupId.set(params['groupId']);
        this.loadGroupData();
      });
  }

  /**
   * Loads all group data including additional data based on state.
   */
  private loadGroupData(): void {
    this.isLoading.set(true);
    this.error.set(null);

    // Fetch group details first
    this.groupService.fetchGroupDetails(this.groupId())
      .pipe(
        switchMap(groupDto => {
          const viewModel = this.groupService.mapToViewModel(groupDto);
          this.groupDetails.set(viewModel);

          // Parallel fetch additional data based on state
          return this.fetchAdditionalData(viewModel);
        }),
        finalize(() => this.isLoading.set(false)),
        catchError(err => {
          this.handleError(err);
          return of(null);
        })
      )
      .subscribe();
  }

  /**
   * Fetches additional data based on group state (pre-draw vs post-draw, organizer vs participant).
   */
  private fetchAdditionalData(viewModel: GroupDetailsViewModel) {
    const requests: any[] = [];

    // Always fetch user's wishlist
    requests.push(
      this.groupService.fetchMyWishlist(this.groupId()).pipe(
        tap(wishlist => this.myWishlist.set(wishlist ?? '')),
        catchError(() => of(null))
      )
    );

    if (!viewModel.drawCompleted) {
      // Pre-draw: fetch budget suggestion
      requests.push(
        this.groupService.fetchMyBudgetSuggestion(this.groupId()).pipe(
          tap(suggestion => this.myBudgetSuggestion.set(suggestion)),
          catchError(() => of(null))
        )
      );

      if (viewModel.isOrganizer) {
        // Organizer: fetch budget suggestions and exclusion rules
        requests.push(
          this.groupService.fetchBudgetSuggestions(this.groupId()).pipe(
            tap(suggestions => this.budgetSuggestions.set(suggestions)),
            catchError(() => of([]))
          )
        );
        requests.push(
          this.groupService.fetchExclusionRules(this.groupId()).pipe(
            tap(rules => this.exclusionRules.set(rules)),
            catchError(() => of([]))
          )
        );
      }
    } else {
      // Post-draw: fetch recipient wishlist
      requests.push(
        this.groupService.fetchRecipientWishlist(this.groupId()).pipe(
          tap(wishlist => this.recipientWishlist.set(wishlist)),
          catchError(() => of(null))
        )
      );
    }

    return forkJoin(requests);
  }

  /**
   * Handles errors from API calls.
   */
  private handleError(err: any): void {
    console.error('Error loading group data:', err);

    if (err.status === 403) {
      this.error.set('Nie masz dostępu do tej grupy.');
      setTimeout(() => this.router.navigate(['/groups']), 3000);
    } else if (err.status === 404) {
      this.error.set('Grupa nie została znaleziona.');
    } else {
      this.error.set('Wystąpił błąd podczas ładowania danych. Spróbuj ponownie.');
    }
  }

  /**
   * Manual refresh handler.
   */
  onRefresh(): void {
    this.isRefreshing.set(true);
    this.loadGroupData();
  }

  /**
   * Dismisses the error alert.
   */
  onDismissError(): void {
    this.error.set(null);
  }

  // ===== Event Handlers =====

  /**
   * Saves the user's wishlist.
   */
  onSaveWishlist(content: string): void {
    if (content.length > 1000) {
      console.error('Wishlist content exceeds 1000 characters');
      return;
    }

    this.isSavingWishlist.set(true);

    this.groupService.updateWishlist(this.groupId(), content)
      .pipe(
        finalize(() => this.isSavingWishlist.set(false)),
        catchError(err => {
          console.error('Failed to save wishlist:', err);
          this.error.set('Nie udało się zapisać listy życzeń. Spróbuj ponownie.');
          return of(null);
        })
      )
      .subscribe(() => {
        this.myWishlist.set(content);
        console.log('Wishlist saved successfully');
      });
  }

  /**
   * Saves the user's budget suggestion.
   */
  onSaveBudget(amount: number | null): void {
    if (amount !== null && (amount <= 0 || !this.hasMaxTwoDecimals(amount))) {
      console.error('Invalid budget amount');
      return;
    }

    this.isSavingBudget.set(true);

    this.groupService.updateBudgetSuggestion(this.groupId(), amount)
      .pipe(
        finalize(() => this.isSavingBudget.set(false)),
        catchError(err => {
          console.error('Failed to save budget suggestion:', err);
          this.error.set('Nie udało się zapisać sugestii budżetu. Spróbuj ponownie.');
          return of(null);
        })
      )
      .subscribe(() => {
        this.myBudgetSuggestion.set(amount);
        console.log('Budget suggestion saved successfully');
      });
  }

  /**
   * Validates that a number has maximum 2 decimal places.
   */
  private hasMaxTwoDecimals(value: number): boolean {
    return /^\d+(\.\d{1,2})?$/.test(value.toString());
  }
}
