export type SiteLanguage = 'fr' | 'en';
export type AttendanceStatus = 'NOT_ANSWERED' | 'ATTENDING' | 'NOT_ATTENDING';

export interface HouseholdGuest {
  id: string;
  type: 'ADULT' | 'CHILD';
  firstName: string;
  lastName: string;
  attendance: AttendanceStatus;
  dietaryRestrictions?: string;
  allergies?: string;
  accessibilityRequirements?: string;
  note?: string;
}

export interface HouseholdAccount {
  id: string;
  householdName: string;
  email: string;
  passwordHash: string;
  passwordSalt: string;
  adults: HouseholdGuest[];
  children: HouseholdGuest[];
  rsvp: Record<string, AttendanceStatus>;
  createdAt: string;
  updatedAt: string;
}

export interface AdminSummary {
  households: number;
  confirmedGuests: number;
  pendingPhotos: number;
  rsvpStatus: string;
}
