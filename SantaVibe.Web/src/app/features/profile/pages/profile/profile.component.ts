import {
  Component,
  OnInit,
  signal,
  inject,
  ChangeDetectionStrategy,
  DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ProfileService } from '../../../../../api/api/profile.service';
import { ProfileViewModel, UpdateProfileRequest } from '../../models/profile.types';
import { mapToProfileViewModel } from '../../utils/profile.mapper';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorAlertComponent } from '../../../../shared/components/error-alert/error-alert.component';
import { ProfileInfoComponent } from '../../components/profile-info/profile-info.component';

/**
 * Profile page component (smart/container component)
 * Manages profile state, API calls, and coordinates child components
 */
@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LoadingSpinnerComponent,
    ErrorAlertComponent,
    ProfileInfoComponent,
    RouterLink
  ]
})
export class ProfileComponent implements OnInit {
  private profileService = inject(ProfileService);
  private destroyRef = inject(DestroyRef);

  // State signals
  protected profile = signal<ProfileViewModel | null>(null);
  protected isLoading = signal<boolean>(true);
  protected isEditing = signal<boolean>(false);
  protected isSaving = signal<boolean>(false);
  protected error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadProfile();
  }

  /**
   * Load user profile from API
   */
  private loadProfile(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.profileService
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const viewModel = mapToProfileViewModel(response);
          this.profile.set(viewModel);
          this.isLoading.set(false);
        },
        error: (err) => {
          this.error.set(
            'Nie udało się załadować profilu. Spróbuj ponownie.'
          );
          this.isLoading.set(false);
          console.error('Profile load error:', err);
        }
      });
  }

  /**
   * Handle save profile event from child component
   */
  protected onSave(request: UpdateProfileRequest): void {
    this.isSaving.set(true);
    this.error.set(null);

    this.profileService
      .updateProfile({ updateProfileRequest: request })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const viewModel = mapToProfileViewModel(response);
          this.profile.set(viewModel);
          this.isSaving.set(false);
          this.isEditing.set(false);
          // TODO: Show success toast when toast service is available
          // this.toastService.showSuccess('Profil zaktualizowany pomyślnie');
        },
        error: (err) => {
          this.isSaving.set(false);
          if (err.status === 400) {
            this.error.set(
              'Walidacja nie powiodła się. Sprawdź wprowadzone dane.'
            );
          } else {
            this.error.set(
              'Nie udało się zaktualizować profilu. Spróbuj ponownie.'
            );
          }
          console.error('Profile update error:', err);
        }
      });
  }

  /**
   * Handle edit event from child component
   */
  protected onEdit(): void {
    this.isEditing.set(true);
  }

  /**
   * Handle cancel edit event from child component
   */
  protected onCancel(): void {
    this.isEditing.set(false);
  }

  /**
   * Handle change password event from child component
   */
  protected onChangePassword(): void {
    // Placeholder - implement when auth endpoint is available
    console.log('Change password clicked');
    // TODO: Implement password reset flow
    // this.authService.initiatePasswordReset()...
  }

  /**
   * Handle error dismissal
   */
  protected onErrorDismiss(): void {
    this.error.set(null);
  }
}
