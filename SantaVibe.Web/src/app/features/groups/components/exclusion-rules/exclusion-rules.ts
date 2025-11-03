import { Component, input, output, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ParticipantViewModel, ExclusionRuleViewModel } from '../../models/group-details.viewmodel';

/**
 * Manages exclusion rules with dropdown form and list of current rules (organizer only, pre-draw).
 * Provides real-time validation feedback to prevent invalid rules.
 */
@Component({
  selector: 'app-exclusion-rules',
  imports: [FormsModule],
  templateUrl: './exclusion-rules.html',
  styleUrl: './exclusion-rules.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExclusionRulesComponent {
  // Input signals
  participants = input.required<ParticipantViewModel[]>();
  currentRules = input.required<ExclusionRuleViewModel[]>();

  // Output signals
  addRule = output<{ user1Id: string; user2Id: string }>();
  deleteRule = output<string>(); // ruleId

  // Internal state
  selectedUser1 = signal<string>('');
  selectedUser2 = signal<string>('');

  // Computed validation
  canAddRule = computed(() => {
    const user1 = this.selectedUser1();
    const user2 = this.selectedUser2();

    if (!user1 || !user2) return false;
    if (user1 === user2) return false;

    const rules = this.currentRules();
    const exists = rules.some(r =>
      (r.user1Id === user1 && r.user2Id === user2) ||
      (r.user1Id === user2 && r.user2Id === user1)
    );

    return !exists;
  });

  validationError = computed(() => {
    const user1 = this.selectedUser1();
    const user2 = this.selectedUser2();

    if (!user1 || !user2) return null;

    if (user1 === user2) {
      return 'Nie można wykluczyć tego samego uczestnika';
    }

    const rules = this.currentRules();
    const exists = rules.some(r =>
      (r.user1Id === user1 && r.user2Id === user2) ||
      (r.user1Id === user2 && r.user2Id === user1)
    );

    if (exists) {
      return 'Ta reguła wykluczenia już istnieje';
    }

    return null;
  });

  onAddRule(): void {
    if (this.canAddRule()) {
      this.addRule.emit({
        user1Id: this.selectedUser1(),
        user2Id: this.selectedUser2()
      });
      // Reset form
      this.selectedUser1.set('');
      this.selectedUser2.set('');
    }
  }
}
