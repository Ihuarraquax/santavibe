export * from './authentication.service';
import { AuthenticationService } from './authentication.service';
export * from './groups.service';
import { GroupsService } from './groups.service';
export * from './invitations.service';
import { InvitationsService } from './invitations.service';
export const APIS = [AuthenticationService, GroupsService, InvitationsService];
