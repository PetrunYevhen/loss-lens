import { computed, Injectable, signal } from '@angular/core';
import { LoginResultDto, UserRole } from '../models/auth.dto';

export interface Session {
  accessToken: string;
  expiresAt: string;
  role: UserRole;
  lineIds: string[];
  username: string;
}

const STORAGE_KEY = 'losslens.session';

function isRole(value: unknown): value is UserRole {
  return value === 'operator' || value === 'supervisor' || value === 'manager';
}

/** Reads a stored session. Anything damaged or expired counts as signed out. */
function load(): Session | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const value = JSON.parse(raw) as Partial<Session>;
    const valid =
      typeof value.accessToken === 'string' &&
      typeof value.username === 'string' &&
      typeof value.expiresAt === 'string' &&
      Date.parse(value.expiresAt) > Date.now() &&
      isRole(value.role) &&
      Array.isArray(value.lineIds) &&
      value.lineIds.every((id) => typeof id === 'string');
    return valid ? (value as Session) : null;
  } catch {
    // Storage can be blocked or hold bad data.
    return null;
  }
}

function save(session: Session | null): void {
  try {
    if (session) {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } else {
      sessionStorage.removeItem(STORAGE_KEY);
    }
  } catch {
    // The session still works for this page load.
  }
}

/**
 * Who is signed in. The session lives in sessionStorage: it survives a reload and is gone when the tab closes.
 * The server decides what the user may see, the role here only drives the labels.
 */
@Injectable({ providedIn: 'root' })
export class AuthState {
  private readonly session = signal<Session | null>(load());

  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly token = computed(() => this.session()?.accessToken ?? null);
  readonly role = computed(() => this.session()?.role ?? null);
  readonly username = computed(() => this.session()?.username ?? null);
  readonly lineIds = computed(() => this.session()?.lineIds ?? []);

  start(result: LoginResultDto, username: string): void {
    const session: Session = { ...result, username };
    this.session.set(session);
    save(session);
  }

  clear(): void {
    this.session.set(null);
    save(null);
  }
}
