import { computed, Injectable, signal } from '@angular/core';

export type UserRole = 'operator' | 'supervisor' | 'manager';

interface StoredRole {
  role: UserRole;
  lineIds: string[];
}

const STORAGE_KEY = 'losslens.role';
const DEFAULT_ROLE: StoredRole = { role: 'manager', lineIds: [] };

function isRole(value: unknown): value is UserRole {
  return value === 'operator' || value === 'supervisor' || value === 'manager';
}

function load(): StoredRole {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Partial<StoredRole>;
      if (isRole(parsed.role) && Array.isArray(parsed.lineIds)) {
        return {
          role: parsed.role,
          lineIds: parsed.lineIds.filter((id) => typeof id === 'string'),
        };
      }
    }
  } catch {
    // Storage can be blocked or hold bad data: fall back to the default role.
  }
  return DEFAULT_ROLE;
}

/** Demo role selection. Not secure: replace with real authentication (JWT) after the MVP. */
@Injectable({ providedIn: 'root' })
export class RoleState {
  private readonly state = signal<StoredRole>(load());

  readonly role = computed(() => this.state().role);
  readonly lineIds = computed(() => this.state().lineIds);

  select(role: UserRole, lineIds: string[]): void {
    const next: StoredRole = { role, lineIds: role === 'manager' ? [] : lineIds };
    this.state.set(next);
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    } catch {
      // The selection still works for this page load.
    }
  }
}
