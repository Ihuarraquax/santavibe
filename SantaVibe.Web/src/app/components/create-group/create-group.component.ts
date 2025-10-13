import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { GroupService } from '../../services/group.service';
import { GroupResponse } from '../../models/group.model';
import { initFlowbite } from 'flowbite';

@Component({
  selector: 'app-create-group',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-group.component.html',
  styleUrl: './create-group.component.css',
})
export class CreateGroupComponent implements OnInit {
  private readonly groupService = inject(GroupService);
  private readonly fb = inject(FormBuilder);

  protected readonly createGroupForm: FormGroup;
  protected readonly isSubmitting = signal(false);
  protected readonly createdGroup = signal<GroupResponse | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    this.createGroupForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(200)]],
    });
  }

  ngOnInit(): void {
    initFlowbite();
  }

  protected onSubmit(): void {
    if (this.createGroupForm.invalid) {
      this.createGroupForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const request = {
      name: this.createGroupForm.value.name,
    };

    this.groupService.createGroup(request).subscribe({
      next: (response) => {
        this.isSubmitting.set(false);
        this.createdGroup.set(response);
        this.createGroupForm.reset();
      },
      error: (error) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          error.error?.message || 'Failed to create group. Please try again.',
        );
        console.error('Error creating group:', error);
      },
    });
  }

  protected copyInvitationLink(): void {
    const group = this.createdGroup();
    if (group) {
      const invitationLink = `${window.location.origin}/join/${group.invitationCode}`;
      navigator.clipboard.writeText(invitationLink).then(
        () => {
          alert('Invitation link copied to clipboard!');
        },
        (err) => {
          console.error('Failed to copy invitation link:', err);
          alert('Failed to copy invitation link. Please copy it manually.');
        },
      );
    }
  }

  protected createAnother(): void {
    this.createdGroup.set(null);
    this.errorMessage.set(null);
  }

  protected get nameControl() {
    return this.createGroupForm.get('name');
  }
}
