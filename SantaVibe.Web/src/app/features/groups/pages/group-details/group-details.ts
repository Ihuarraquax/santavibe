import { Component, OnInit, inject, signal, computed, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, switchMap, finalize, tap, catchError, of } from 'rxjs';
import { GroupService } from '../../services/group.service';
import {
  GroupDetailsViewModel,
  ExclusionRuleViewModel
} from '../../models/group-details.viewmodel';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorAlertComponent } from '../../../../shared/components/error-alert/error-alert.component';
import { GroupInfoCardComponent } from '../../components/group-info-card/group-info-card';
import { ParticipantListComponent } from '../../components/participant-list/participant-list';
import { WishlistEditorComponent } from '../../components/wishlist-editor/wishlist-editor';
import { BudgetSuggestionComponent } from '../../components/budget-suggestion/budget-suggestion';
import { InvitationLinkCard } from '../../components/invitation-link-card/invitation-link-card';
import { BudgetManagementComponent } from '../../components/budget-management/budget-management';
import { ExclusionRulesComponent } from '../../components/exclusion-rules/exclusion-rules';
import { DrawValidationComponent } from '../../components/draw-validation/draw-validation';
import { DrawExecutionComponent } from '../../components/draw-execution/draw-execution';
import { AssignmentCardComponent } from '../../components/assignment-card/assignment-card';
import { GiftSuggestionsComponent } from '../../components/gift-suggestions/gift-suggestions';

/**
 * Main container component for the Group Details view.
 * Orchestrates all data fetching, state management, and user interactions.
 * Conditionally renders child components based on user role and draw state.
 */
@Component({
  selector: 'app-group-details',
  imports: [
    DecimalPipe,
    LoadingSpinnerComponent,
    ErrorAlertComponent,
    GroupInfoCardComponent,
    ParticipantListComponent,
    WishlistEditorComponent,
    BudgetSuggestionComponent,
    InvitationLinkCard,
    BudgetManagementComponent,
    ExclusionRulesComponent,
    DrawValidationComponent,
    DrawExecutionComponent,
    AssignmentCardComponent,
    GiftSuggestionsComponent
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
  giftSuggestionsMarkdown = signal<string>('');
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

    if (!viewModel.drawCompleted) {
      // Pre-draw: fetch budget suggestion only
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
      // Post-draw: fetch user's wishlist and recipient wishlist
      requests.push(
        this.groupService.fetchMyWishlist(this.groupId()).pipe(
          tap(wishlist => this.myWishlist.set(wishlist ?? '')),
          catchError(() => of(null))
        )
      );
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

    this.groupService.fetchGroupDetails(this.groupId())
      .pipe(
        switchMap(groupDto => {
          const viewModel = this.groupService.mapToViewModel(groupDto);
          this.groupDetails.set(viewModel);

          return this.fetchAdditionalData(viewModel);
        }),
        finalize(() => this.isRefreshing.set(false)),
        catchError(err => {
          this.handleError(err);
          return of(null);
        })
      )
      .subscribe();
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

  /**
   * Adds an exclusion rule (organizer only).
   */
  onAddExclusionRule(data: { user1Id: string; user2Id: string }): void {
    this.groupService.createExclusionRule(this.groupId(), data.user1Id, data.user2Id)
      .pipe(
        switchMap(() => forkJoin([
          this.groupService.fetchExclusionRules(this.groupId()),
          this.groupService.fetchGroupDetails(this.groupId())
        ])),
        catchError(err => {
          console.error('Failed to add exclusion rule:', err);
          this.error.set('Nie udało się dodać reguły wykluczenia. Spróbuj ponownie.');
          return of(null);
        })
      )
      .subscribe(result => {
        if (result) {
          const [rules, groupDto] = result;
          this.exclusionRules.set(rules);
          this.groupDetails.set(this.groupService.mapToViewModel(groupDto));
          console.log('Exclusion rule added successfully');
        }
      });
  }

  /**
   * Deletes an exclusion rule (organizer only).
   */
  onDeleteExclusionRule(ruleId: string): void {
    this.groupService.deleteExclusionRule(this.groupId(), ruleId)
      .pipe(
        switchMap(() => forkJoin([
          this.groupService.fetchExclusionRules(this.groupId()),
          this.groupService.fetchGroupDetails(this.groupId())
        ])),
        catchError(err => {
          console.error('Failed to delete exclusion rule:', err);
          this.error.set('Nie udało się usunąć reguły wykluczenia. Spróbuj ponownie.');
          return of(null);
        })
      )
      .subscribe(result => {
        if (result) {
          const [rules, groupDto] = result;
          this.exclusionRules.set(rules);
          this.groupDetails.set(this.groupService.mapToViewModel(groupDto));
          console.log('Exclusion rule deleted successfully');
        }
      });
  }

  /**
   * Executes the Secret Santa draw (organizer only).
   */
  onExecuteDraw(budget: number): void {
    this.isExecutingDraw.set(true);

    this.groupService.executeDraw(this.groupId(), budget)
      .pipe(
        switchMap(() => this.groupService.fetchGroupDetails(this.groupId())),
        tap(groupDto => {
          this.groupDetails.set(this.groupService.mapToViewModel(groupDto));
          console.log('Draw completed successfully');
        }),
        switchMap(() => this.groupService.fetchRecipientWishlist(this.groupId())),
        tap(wishlist => this.recipientWishlist.set(wishlist)),
        finalize(() => this.isExecutingDraw.set(false)),
        catchError(err => {
          console.error('Failed to execute draw:', err);
          this.error.set('Nie udało się wykonać losowania. Spróbuj ponownie.');
          return of(null);
        })
      )
      .subscribe();
  }

  /**
   * Generates AI gift suggestions in markdown format (post-draw).
   */
  onGenerateGiftSuggestions(): void {
    this.isGeneratingSuggestions.set(true);

    this.groupService.generateGiftSuggestions(this.groupId())
      .pipe(
        finalize(() => this.isGeneratingSuggestions.set(false)),
        catchError(err => {
          console.error('Failed to generate gift suggestions:', err);
          this.error.set('Nie udało się wygenerować propozycji prezentów. Spróbuj ponownie.');
          return of('');
        })
      )
      .subscribe(markdown => {
        this.giftSuggestionsMarkdown.set(markdown);
        console.log('Gift suggestions generated successfully');
      });
  }

  /**
   * Cancels gift suggestion generation (post-draw).
   */
  onCancelGiftSuggestions(): void {
    // For now, we just reset the loading state
    // In a real implementation, you'd cancel the HTTP request
    this.isGeneratingSuggestions.set(false);
  }
}
