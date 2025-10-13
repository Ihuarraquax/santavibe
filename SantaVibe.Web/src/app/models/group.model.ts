export interface Group {
  id: string;
  name: string;
  description?: string;
  budget?: number;
  drawDate?: Date;
  createdAt: Date;
  updatedAt: Date;
  organizerId: string;
  isDrawCompleted: boolean;
  invitationCode?: string;
}

export interface CreateGroupRequest {
  name: string;
  description?: string;
  budget?: number;
  drawDate?: Date;
}
