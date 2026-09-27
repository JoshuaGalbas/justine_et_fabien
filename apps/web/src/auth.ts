import { apiRequest } from './api';

export type AttendanceStatus = 'NOT_ANSWERED' | 'ATTENDING' | 'NOT_ATTENDING';
export type GuestType = 'ADULT' | 'CHILD';

export interface HouseholdGuest {
  id: string;
  type: GuestType;
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

const SESSION_KEY = 'justine-et-fabien-session';

function normalizeEmail(value: string): string {
  return value.trim().toLowerCase();
}

function readStorage<T>(key: string): T | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const value = window.localStorage.getItem(key);
  return value ? (JSON.parse(value) as T) : null;
}

function writeStorage<T>(key: string, value: T) {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.setItem(key, JSON.stringify(value));
}

export function getCurrentUserId(): string | null {
  return readStorage<string | null>(SESSION_KEY) ?? null;
}

export function setCurrentUserId(userId: string | null) {
  if (userId) {
    writeStorage(SESSION_KEY, userId);
    return;
  }

  if (typeof window !== 'undefined') {
    window.localStorage.removeItem(SESSION_KEY);
  }
}

export function getCurrentUser(): HouseholdAccount | null {
  const userId = getCurrentUserId();
  if (!userId) {
    return null;
  }

  const stored = readStorage<HouseholdAccount | null>('justine-et-fabien-current-user');
  if (stored && stored.id === userId) {
    return stored;
  }

  return null;
}

function saveCurrentUser(user: HouseholdAccount | null) {
  if (user) {
    writeStorage('justine-et-fabien-current-user', user);
    setCurrentUserId(user.id);
    return;
  }

  if (typeof window !== 'undefined') {
    window.localStorage.removeItem('justine-et-fabien-current-user');
  }
  setCurrentUserId(null);
}

export async function registerHousehold(input: {
  householdName: string;
  email: string;
  password: string;
  confirmPassword: string;
}): Promise<{ success: boolean; message: string; user?: HouseholdAccount }> {
  const householdName = input.householdName.trim();
  const email = normalizeEmail(input.email);

  if (!householdName || !email || !input.password) {
    return { success: false, message: 'Household name, email, and password are required.' };
  }

  if (input.password !== input.confirmPassword) {
    return { success: false, message: 'Password and password confirmation do not match.' };
  }

  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return { success: false, message: 'Please enter a valid email address.' };
  }

  if (input.password.length < 8) {
    return { success: false, message: 'Password must contain at least 8 characters.' };
  }

  try {
    const result = await apiRequest<{ accountId: string; householdName: string }>('auth/register', {
      method: 'POST',
      body: JSON.stringify({ householdName, email, password: input.password }),
    });

    const user: HouseholdAccount = {
      id: result.accountId,
      householdName: result.householdName,
      email,
      passwordHash: '',
      passwordSalt: '',
      adults: [{ id: crypto.randomUUID(), type: 'ADULT', firstName: '', lastName: '', attendance: 'NOT_ANSWERED' }],
      children: [],
      rsvp: { ceremony: 'NOT_ANSWERED', dinner: 'NOT_ANSWERED', brunch: 'NOT_ANSWERED' },
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    saveCurrentUser(user);
    return { success: true, message: 'Account created successfully.', user };
  } catch (error) {
    return { success: false, message: error instanceof Error ? error.message : 'Registration failed.' };
  }
}

export async function signInHousehold(input: { email: string; password: string }): Promise<{ success: boolean; message: string; user?: HouseholdAccount }> {
  const email = normalizeEmail(input.email);

  if (!email || !input.password) {
    return { success: false, message: 'Email and password are required.' };
  }

  try {
    const result = await apiRequest<{ accountId: string; householdName: string }>('auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password: input.password }),
    });

    const user: HouseholdAccount = {
      id: result.accountId,
      householdName: result.householdName,
      email,
      passwordHash: '',
      passwordSalt: '',
      adults: [{ id: crypto.randomUUID(), type: 'ADULT', firstName: '', lastName: '', attendance: 'NOT_ANSWERED' }],
      children: [],
      rsvp: { ceremony: 'NOT_ANSWERED', dinner: 'NOT_ANSWERED', brunch: 'NOT_ANSWERED' },
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    saveCurrentUser(user);
    return { success: true, message: 'Welcome back.', user };
  } catch (error) {
    return { success: false, message: error instanceof Error ? error.message : 'Login failed.' };
  }
}

export function signOutHousehold() {
  saveCurrentUser(null);
}

export function updateHousehold(user: HouseholdAccount) {
  saveCurrentUser(user);
}
