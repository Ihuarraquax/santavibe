import { Component, ChangeDetectionStrategy, signal, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { GroupService } from '../../services/group.service';
import { GroupCardViewModel, DrawStatus } from '../../models/group.types';
import { GroupDto } from '@api/model/group-dto';
import { format, formatDistance } from 'date-fns';
import { pl } from 'date-fns/locale';
import { HttpErrorResponse } from '@angular/common/http';
import { GroupCardComponent } from '../../components/group-card/group-card.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ErrorAlertRetryComponent } from '../../../../shared/components/error-alert-retry/error-alert-retry.component';

/**
 * The main container component responsible for fetching and displaying the user's groups.
 * It manages loading states, handles errors, orchestrates child components, and provides navigation to group details and group creation.
 * Uses Angular Signals for reactive state management and OnPush change detection strategy.
 */
@Component({
  selector: 'app-groups-list',
  templateUrl: './groups-list.component.html',
  styleUrls: ['./groups-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    GroupCardComponent,
    SkeletonLoaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    ErrorAlertRetryComponent
  ]
})
export class GroupsListComponent {
  // Injected services
  private groupService = inject(GroupService);
  private router = inject(Router);

  // Local state signals
  isLoading = signal<boolean>(true);
  isRefreshing = signal<boolean>(false);
  error = signal<string | null>(null);
  lastUpdated = signal<Date | null>(null);

  // Computed from service
  groups = computed(() => this.groupService.groups());

  // View models
  groupViewModels = computed(() =>
    this.groups()
      .map(group => this.transformToViewModel(group))
      .filter((vm): vm is GroupCardViewModel => vm !== null)
  );

  async ngOnInit(): Promise<void> {
    await this.loadGroups();
  }

  private async loadGroups(): Promise<void> {
    try {
      this.isLoading.set(true);
      this.error.set(null);
      await this.groupService.fetchGroups();
      this.lastUpdated.set(new Date());
    } catch (err) {
      this.error.set(this.mapError(err));
    } finally {
      this.isLoading.set(false);
    }
  }

  async handleRefresh(): Promise<void> {
    try {
      this.isRefreshing.set(true);
      this.error.set(null);
      await this.groupService.fetchGroups();
      this.lastUpdated.set(new Date());
    } catch (err) {
      this.error.set(this.mapError(err));
    } finally {
      this.isRefreshing.set(false);
    }
  }

  navigateToGroup(groupId: string): void {
    this.router.navigate(['/groups', groupId]);
  }

  navigateToCreate(): void {
    this.router.navigate(['/groups/create']);
  }

  clearError(): void {
    this.error.set(null);
  }

  private transformToViewModel(dto: GroupDto): GroupCardViewModel | null {
    try {
      return {
        groupId: dto.groupId,
        name: dto.name || 'Grupa bez nazwy',
        organizerName: dto.organizerName || 'Nieznany',
        participantCount: dto.participantCount || 0,
        budget: dto.budget ? this.formatBudget(dto.budget) : null,
        drawStatus: dto.drawCompleted ? DrawStatus.Completed : DrawStatus.Pending,
        drawStatusLabel: dto.drawCompleted ? 'Losowanie zakończone' : 'Oczekuje na losowanie',
        drawStatusColor: dto.drawCompleted ? 'success' : 'warning',
        joinedDate: this.formatDate(dto.joinedAt),
        drawCompletedDate: dto.drawCompletedAt ? this.formatDate(dto.drawCompletedAt) : null,
        isOrganizer: dto.isOrganizer
      };
    } catch (err) {
      console.error('Failed to transform group:', dto, err);
      return null;
    }
  }

  formatBudget(budget: number | null | undefined): string | null {
    if (budget == null || isNaN(budget)) return null;

    try {
      return new Intl.NumberFormat('pl-PL', {
        style: 'currency',
        currency: 'PLN'
      }).format(budget);
    } catch (err) {
      console.error('Failed to format budget:', budget, err);
      return `${budget} PLN`;
    }
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return '';

    try {
      const date = new Date(dateString);
      if (isNaN(date.getTime())) {
        return 'Nieprawidłowa data';
      }
      return format(date, 'd MMM yyyy', { locale: pl });
    } catch (err) {
      console.error('Failed to format date:', dateString, err);
      return 'Nieprawidłowa data';
    }
  }

  formatRelativeTime(date: Date | null): string {
    if (!date) return '';

    try {
      return formatDistance(date, new Date(), {
        addSuffix: true,
        locale: pl
      });
    } catch (err) {
      console.error('Failed to format relative time:', date, err);
      return '';
    }
  }

  private mapError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 0:
          return 'Brak połączenia z internetem. Sprawdź swoje połączenie.';
        case 401:
          return 'Sesja wygasła. Zaloguj się ponownie.';
        case 403:
          return 'Nie masz uprawnień do wykonania tej operacji.';
        case 404:
          return 'Nie znaleziono żądanych danych.';
        case 500:
        case 502:
        case 503:
          return 'Wystąpił błąd serwera. Spróbuj ponownie później.';
        default:
          return 'Wystąpił nieoczekiwany błąd. Spróbuj ponownie.';
      }
    }

    if (error instanceof Error) {
      return error.message;
    }

    return 'Wystąpił nieoczekiwany błąd.';
  }
}
