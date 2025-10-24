import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ParticipantViewModel } from '../../models/group-details.viewmodel';

/**
 * Presentational component that displays a list of all participants in the group.
 * Shows participant information and optional remove button for organizers.
 */
@Component({
  selector: 'app-participant-list',
  imports: [DatePipe],
  templateUrl: './participant-list.html',
  styleUrl: './participant-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ParticipantListComponent {
  participants = input.required<ParticipantViewModel[]>();
  canRemove = input<boolean>(false);
  removeParticipant = output<string>(); // userId
}
