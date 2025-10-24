import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';

/**
 * Presentational component that displays read-only summary information about the group.
 * Shows group name, organizer, participant count, and relevant dates.
 */
@Component({
  selector: 'app-group-info-card',
  imports: [DatePipe],
  templateUrl: './group-info-card.html',
  styleUrl: './group-info-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GroupInfoCardComponent {
  groupName = input.required<string>();
  organizerName = input.required<string>();
  participantCount = input.required<number>();
  createdAt = input.required<Date>();
  drawCompletedAt = input<Date | null>(null);
}
