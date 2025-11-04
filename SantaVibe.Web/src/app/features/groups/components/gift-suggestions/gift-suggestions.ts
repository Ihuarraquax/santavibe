import { Component, input, output, computed, ChangeDetectionStrategy, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

/**
 * Provides AI gift suggestions in markdown format (post-draw).
 * Shows generate button, loading state, and renders markdown suggestions as HTML.
 */
@Component({
  selector: 'app-gift-suggestions',
  imports: [LoadingSpinnerComponent],
  templateUrl: './gift-suggestions.html',
  styleUrl: './gift-suggestions.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GiftSuggestionsComponent {
  private sanitizer = inject(DomSanitizer);

  // Input signals
  suggestionsMarkdown = input<string>('');
  isGenerating = input<boolean>(false);
  error = input<string | null>(null);

  // Output signals
  generate = output<void>();
  cancel = output<void>();

  // Computed signal to convert markdown to sanitized HTML
  suggestionsHtml = computed<SafeHtml>(() => {
    const markdown = this.suggestionsMarkdown();
    if (!markdown) {
      return '';
    }

    try {
      // Parse markdown to HTML
      const html = marked.parse(markdown, { async: false }) as string;
      // Sanitize HTML to prevent XSS
      return this.sanitizer.sanitize(1, html) || '';
    } catch (error) {
      console.error('Error parsing markdown:', error);
      return '';
    }
  });

  // Helper computed to check if suggestions exist
  hasSuggestions = computed(() => this.suggestionsMarkdown().length > 0);
}
