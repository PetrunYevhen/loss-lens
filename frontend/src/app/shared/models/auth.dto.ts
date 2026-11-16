export type UserRole = 'operator' | 'supervisor' | 'manager';

export interface LoginResultDto {
  accessToken: string;
  expiresAt: string;
  role: UserRole;
  /** Lines the user may see. Empty for a manager. */
  lineIds: string[];
}
