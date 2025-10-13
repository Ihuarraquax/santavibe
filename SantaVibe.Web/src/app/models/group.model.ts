export interface CreateGroupRequest {
  name: string;
}

export interface GroupResponse {
  id: string;
  name: string;
  organizerId: string;
  organizerName: string;
  invitationCode: string;
  budget: number | null;
  isDrawPerformed: boolean;
  createdAt: Date;
  drawPerformedAt: Date | null;
  participantCount: number;
}
