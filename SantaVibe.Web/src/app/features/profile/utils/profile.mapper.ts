import { GetProfileResponse, ProfileViewModel } from '../models/profile.types';
import { formatDateInPolish } from '../../../shared/utils/date-format.util';

/**
 * Transform GetProfileResponse DTO to ProfileViewModel for display
 *
 * @param dto Response from GET /api/profile
 * @returns View model with formatted dates
 */
export function mapToProfileViewModel(dto: GetProfileResponse): ProfileViewModel {
  return {
    userId: dto.userId ?? '',
    email: dto.email ?? '',
    firstName: dto.firstName ?? '',
    lastName: dto.lastName ?? '',
    createdAt: formatDateInPolish(dto.createdAt),
    lastLoginAt: formatDateInPolish(dto.lastLoginAt),
    createdAtRaw: dto.createdAt ? new Date(dto.createdAt) : new Date(),
    lastLoginAtRaw: dto.lastLoginAt ? new Date(dto.lastLoginAt) : new Date()
  };
}
