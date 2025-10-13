import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GroupService } from '../../services/group.service';
import { CreateGroupRequest } from '../../models/group.model';

@Component({
  selector: 'app-add-group',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-group.component.html',
  styleUrl: './add-group.component.css'
})
export class AddGroupComponent {
  protected name = signal('');
  protected description = signal('');
  protected budget = signal<number | undefined>(undefined);
  protected drawDate = signal<string>('');
  protected isSubmitting = signal(false);
  protected errorMessage = signal<string | null>(null);
  protected successMessage = signal<string | null>(null);

  constructor(private groupService: GroupService) {}

  onSubmit(): void {
    if (this.isSubmitting()) return;

    // Reset messages
    this.errorMessage.set(null);
    this.successMessage.set(null);

    // Validate required fields
    if (!this.name().trim()) {
      this.errorMessage.set('Group name is required');
      return;
    }

    if (this.name().trim().length < 3) {
      this.errorMessage.set('Group name must be at least 3 characters');
      return;
    }

    this.isSubmitting.set(true);

    const request: CreateGroupRequest = {
      name: this.name().trim(),
      description: this.description().trim() || undefined,
      budget: this.budget(),
      drawDate: this.drawDate() ? new Date(this.drawDate()) : undefined
    };

    this.groupService.createGroup(request).subscribe({
      next: (group) => {
        this.successMessage.set(`Group "${group.name}" created successfully! Invitation code: ${group.invitationCode}`);
        this.resetForm();
        this.isSubmitting.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.message);
        this.isSubmitting.set(false);
      }
    });
  }

  private resetForm(): void {
    this.name.set('');
    this.description.set('');
    this.budget.set(undefined);
    this.drawDate.set('');
  }

  protected get minDate(): string {
    const today = new Date();
    today.setDate(today.getDate() + 1);
    return today.toISOString().split('T')[0];
  }
}
