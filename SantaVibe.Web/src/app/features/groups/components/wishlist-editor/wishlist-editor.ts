import { Component, input, output, signal, computed, effect, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

/**
 * Presentational component for creating and editing wishlist.
 * Validates character count and tracks dirty state.
 */
@Component({
  selector: 'app-wishlist-editor',
  imports: [FormsModule, LoadingSpinnerComponent],
  templateUrl: './wishlist-editor.html',
  styleUrl: './wishlist-editor.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WishlistEditorComponent {
  // Input signals
  initialContent = input<string>('');
  isEditable = input<boolean>(true);
  isSaving = input<boolean>(false);
  helperText = input<string | null>(null);

  // Output signals
  save = output<string>();

  // Internal state
  wishlistContent = signal<string>('');

  // Computed signals
  characterCount = computed(() => this.wishlistContent().length);
  isValid = computed(() => this.characterCount() <= 1000);
  isDirty = computed(() => this.wishlistContent() !== this.initialContent());
  validationError = computed(() =>
    this.isValid() ? null : 'Lista życzeń nie może przekraczać 1000 znaków'
  );
  canSave = computed(() =>
    this.isValid() && this.isDirty() && !this.isSaving()
  );

  constructor() {
    // Sync initial content to internal signal
    effect(() => {
      this.wishlistContent.set(this.initialContent());
    });
  }

  onContentChange(value: string): void {
    this.wishlistContent.set(value);
  }

  onSave(): void {
    if (this.canSave()) {
      this.save.emit(this.wishlistContent());
    }
  }
}
