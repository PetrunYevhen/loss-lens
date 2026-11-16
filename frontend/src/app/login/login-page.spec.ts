import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthState } from '../shared/auth/auth-state';
import { UK } from '../shared/i18n/uk';
import { LoginPage } from './login-page';

@Component({ template: '' })
class Blank {}

describe('LoginPage', () => {
  let backend: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: '**', component: Blank }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => sessionStorage.clear());

  function render(): {
    element: HTMLElement;
    refresh: () => void;
    submit: (user: string, password: string) => void;
  } {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    return {
      element,
      refresh: () => fixture.detectChanges(),
      submit: (user, password) => {
        (element.querySelector('#username') as HTMLInputElement).value = user;
        (element.querySelector('#password') as HTMLInputElement).value = password;
        element.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
        fixture.detectChanges();
      },
    };
  }

  it('asks for both fields before calling the server', () => {
    const { element, submit } = render();

    submit('operator1', '');

    backend.expectNone('/api/v1/auth/login');
    expect(element.querySelector('[role="alert"]')?.textContent).toContain(UK.login.required);
  });

  it('signs in and stores the session', () => {
    const { submit } = render();

    submit(' Operator1 ', 'secret');
    const request = backend.expectOne('/api/v1/auth/login');
    expect(request.request.body).toEqual({ username: 'Operator1', password: 'secret' });
    request.flush({
      accessToken: 'tok',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      role: 'operator',
      lineIds: ['l1'],
    });

    const auth = TestBed.inject(AuthState);
    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.username()).toBe('operator1');
    expect(auth.role()).toBe('operator');
  });

  it('shows a clear message for wrong credentials and stays signed out', () => {
    const { element, submit, refresh } = render();

    submit('operator1', 'wrong');
    backend.expectOne('/api/v1/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    refresh();

    expect(TestBed.inject(AuthState).isAuthenticated()).toBe(false);
    expect(element.textContent).toContain(UK.login.invalid);
  });

  it('shows another message when the server cannot be reached', () => {
    const { element, submit, refresh } = render();

    submit('operator1', 'secret');
    backend.expectOne('/api/v1/auth/login').error(new ProgressEvent('error'));
    refresh();

    expect(element.textContent).toContain(UK.login.unreachable);
  });

  it('does not follow a return address that leaves the app', async () => {
    const spy = vi.spyOn(router, 'navigateByUrl');
    const { submit } = render();

    submit('operator1', 'secret');
    backend.expectOne('/api/v1/auth/login').flush({
      accessToken: 'tok',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      role: 'manager',
      lineIds: [],
    });

    expect(spy).toHaveBeenCalledWith('/');
  });
});
