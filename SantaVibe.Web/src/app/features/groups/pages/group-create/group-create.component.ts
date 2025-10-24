import {
  Component,
  ChangeDetectionStrategy,
  signal,
  inject,
  ViewChild,
  ElementRef,
  afterNextRender
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormControl, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { GroupsService } from '@api/api/groups.service';
import { CreateGroupRequest } from '@api/model/create-group-request';
import { CreateGroupResponse } from '@api/model/create-group-response';
import { ErrorAlertComponent } from '../../../../shared/components/error-alert/error-alert.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

/**
 * Component for creating a new Secret Santa group.
 * Uses Reactive Forms for form state management and signals for non-form state.
 * After successful creation, redirects to the groups list page with a success notification.
 */
@Component({
  selector: 'app-group-create',
  templateUrl: './group-create.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ErrorAlertComponent,
    LoadingSpinnerComponent
  ]
})
export class GroupCreateComponent {
  // Injected Services
  private groupsService = inject(GroupsService);
  private toastr = inject(ToastrService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  // View Children
  @ViewChild('groupNameInput') groupNameInput?: ElementRef<HTMLInputElement>;

  // Reactive Form
  form = this.fb.group({
    groupName: ['', [
      Validators.required,
      Validators.minLength(3),
      Validators.maxLength(200)
    ]]
  });

  // Convenience getter for form control
  get groupNameControl(): FormControl {
    return this.form.get('groupName') as FormControl;
  }

  // State Signals (only for non-form state)
  isLoading = signal<boolean>(false);
  error = signal<string | null>(null);

  // Lifecycle Hooks
  constructor() {
    // Auto-focus on group name input after view init
    afterNextRender(() => {
      this.groupNameInput?.nativeElement.focus();
    });
  }

  // Methods
  onSubmit(): void {
    if (this.form.invalid) {
      this.groupNameControl.markAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);

    const request: CreateGroupRequest = {
      name: this.groupNameControl.value.trim()
    };

    this.groupsService.createGroup({ createGroupRequest: request }).subscribe({
      next: (response: CreateGroupResponse) => {
        this.isLoading.set(false);
        this.form.markAsPristine();

        // Show success toast notification
        this.toastr.success(
          'Skopiuj link zaproszenia, aby zaprosić uczestników',
          'Grupa została utworzona! 🎄',
          { timeOut: 7000 }
        );

        // Navigate back to groups list
        this.router.navigate(['/groups']);
      },
      error: (error: any) => {
        this.isLoading.set(false);
        const errorMessage = this.parseErrorMessage(error);
        this.error.set(errorMessage);
      }
    });
  }

  clearError(): void {
    this.error.set(null);
  }

  getErrorMessage(): string {
    const control = this.groupNameControl;

    if (control.hasError('required')) {
      return 'Nazwa grupy jest wymagana';
    }

    if (control.hasError('minlength')) {
      return 'Nazwa grupy musi mieć co najmniej 3 znaki';
    }

    if (control.hasError('maxlength')) {
      return 'Nazwa grupy nie może przekraczać 200 znaków';
    }

    return '';
  }

  canDeactivate(): boolean {
    if (this.form.pristine) {
      return true;
    }

    return window.confirm(
      'Masz niezapisane zmiany. Czy na pewno chcesz opuścić stronę?'
    );
  }

  private parseErrorMessage(error: any): string {
    // Network error
    if (error.status === 0) {
      return 'Nie można połączyć się z serwerem. Sprawdź połączenie internetowe.';
    }

    // Validation error with field details
    if (error.status === 400 && error.error?.details?.name) {
      return error.error.details.name[0];
    }

    // Generic validation error
    if (error.status === 400) {
      return 'Błąd walidacji. Sprawdź wprowadzone dane.';
    }

    // Rate limiting
    if (error.status === 429) {
      return 'Zbyt wiele żądań. Poczekaj chwilę i spróbuj ponownie.';
    }

    // Server error
    if (error.status >= 500) {
      return 'Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.';
    }

    // Extract message from error object
    if (error.error?.message) {
      return error.error.message;
    }

    // Generic fallback
    return 'Nie udało się utworzyć grupy. Spróbuj ponownie.';
  }
}
