import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthState } from './auth-state';
import { authInterceptor } from './auth.interceptor';

@Component({ template: '' })
class Blank {}

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: AuthState;
  let router: Router;

  beforeEach(async () => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: '**', component: Blank }]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthState);
    router = TestBed.inject(Router);
    await router.navigateByUrl('/fpy?line=L1');
  });

  afterEach(() => sessionStorage.clear());

  function signIn(): void {
    auth.start(
      { accessToken: 'abc', expiresAt: new Date(Date.now() + 3_600_000).toISOString(), role: 'manager', lineIds: [] },
      'manager',
    );
  }

  it('sends the token to the API', () => {
    signIn();

    http.get('/api/v1/lines').subscribe();

    expect(backend.expectOne('/api/v1/lines').request.headers.get('Authorization')).toBe('Bearer abc');
  });

  it('does not send the token anywhere else', () => {
    signIn();

    http.get('https://example.com/data').subscribe();

    expect(backend.expectOne('https://example.com/data').request.headers.has('Authorization')).toBe(false);
  });

  it('does not attach the token to the sign-in request', () => {
    signIn();

    http.post('/api/v1/auth/login', {}).subscribe();

    expect(backend.expectOne('/api/v1/auth/login').request.headers.has('Authorization')).toBe(false);
  });

  it('signs out and returns to the sign-in page on 401, remembering where the user was', () => {
    signIn();
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.get('/api/v1/lines').subscribe({ error: () => undefined });
    backend.expectOne('/api/v1/lines').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(auth.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/fpy?line=L1' } });
  });

  it('leaves a failed sign-in to the form', () => {
    signIn();
    let status = 0;

    http.post('/api/v1/auth/login', {}).subscribe({ error: (e) => (status = e.status) });
    backend.expectOne('/api/v1/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(status).toBe(401);
    expect(auth.isAuthenticated()).toBe(true);
  });

  it('does not sign out on other errors', () => {
    signIn();

    http.get('/api/v1/lines').subscribe({ error: () => undefined });
    backend.expectOne('/api/v1/lines').flush({}, { status: 403, statusText: 'Forbidden' });

    expect(auth.isAuthenticated()).toBe(true);
  });
});
