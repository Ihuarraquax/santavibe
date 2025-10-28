import { Component, input, signal, ChangeDetectionStrategy } from '@angular/core';

/**
 * Component for displaying and copying the group invitation link.
 * Only shown to organizers before the draw.
 */
@Component({
  selector: 'app-invitation-link-card',
  imports: [],
  templateUrl: './invitation-link-card.html',
  styleUrl: './invitation-link-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InvitationLinkCard {
  /** The invitation link to display and copy */
  invitationLink = input.required<string>();

  /** Signal to track whether the link was recently copied */
  isCopied = signal<boolean>(false);

  /**
   * Copies the invitation link to the clipboard.
   */
  async copyToClipboard(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.invitationLink());
      this.isCopied.set(true);

      // Reset the copied state after 2 seconds
      setTimeout(() => {
        this.isCopied.set(false);
      }, 2000);
    } catch (err) {
      console.error('Failed to copy invitation link:', err);
    }
  }
}
