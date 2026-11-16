import { TestBed } from '@angular/core/testing';
import { LoginResultDto } from '../models/auth.dto';
import { AuthState } from './auth-state';

const KEY = 'losslens.session';

function result(overrides: Partial<LoginResultDto> = {}): LoginResultDto {
  return {
    accessToken: 'token-1',
    expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
    role: 'operator',
    lineIds: ['line-1'],
    ...overrides,
  };
}

describe('AuthState', () => {
  beforeEach(() => sessionStorage.clear());
  afterEach(() => sessionStorage.clear());

  const create = () => TestBed.inject(AuthState);

  it('starts signed out', () => {
    const auth = create();

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.token()).toBeNull();
    expect(auth.role()).toBeNull();
    expect(auth.lineIds()).toEqual([]);
  });

  it('keeps the session after sign-in', () => {
    const auth = create();

    auth.start(result(), 'operator1');

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.token()).toBe('token-1');
    expect(auth.role()).toBe('operator');
    expect(auth.username()).toBe('operator1');
    expect(auth.lineIds()).toEqual(['line-1']);
  });

  it('restores the session after a reload', () => {
    create().start(result(), 'operator1');
    TestBed.resetTestingModule();

    const restored = create();

    expect(restored.isAuthenticated()).toBe(true);
    expect(restored.username()).toBe('operator1');
  });

  it('forgets the session on sign-out, also in storage', () => {
    const auth = create();
    auth.start(result(), 'operator1');

    auth.clear();

    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem(KEY)).toBeNull();
  });

  it('ignores an expired stored session', () => {
    sessionStorage.setItem(
      KEY,
      JSON.stringify({ ...result({ expiresAt: new Date(Date.now() - 1000).toISOString() }), username: 'a' }),
    );

    expect(create().isAuthenticated()).toBe(false);
  });

  it('ignores damaged stored data', () => {
    sessionStorage.setItem(KEY, '{not json');
    expect(create().isAuthenticated()).toBe(false);

    TestBed.resetTestingModule();
    sessionStorage.setItem(KEY, JSON.stringify({ accessToken: 'x', role: 'admin' }));
    expect(create().isAuthenticated()).toBe(false);
  });
});
