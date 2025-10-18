import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { GroupCardViewModel, BadgeColor } from '../../models/group.types';

/**
 * A reusable presentational component that displays a single group's summary information in a card format.
 * The card is clickable and shows key group details including name, organizer, participant count, budget, and draw status.
 * Uses Flowbite card styling with Tailwind CSS.
 */
@Component({
  selector: 'app-group-card',
  templateUrl: './group-card.component.html',
  styleUrls: ['./group-card.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GroupCardComponent {
  // Input
  group = input.required<GroupCardViewModel>();

  // Output
  cardClick = output<string>(); // Emits groupId

  handleClick(): void {
    this.cardClick.emit(this.group().groupId);
  }

  getBadgeClasses(color: BadgeColor): string {
    const classes = {
      success: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
      warning: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300',
      info: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300',
      error: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300'
    };
    return classes[color] || classes.info;
  }
}
