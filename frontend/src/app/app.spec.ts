import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { AuthState } from './shared/auth/auth-state';
import { UK } from './shared/i18n/uk';

@Component({ template: '' })
class Blank {}

describe('App', () => {
  beforeEach(() => sessionStorage.clear());
  afterEach(() => sessionStorage.clear());

  function render(signedIn: boolean): HTMLElement {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: '**', component: Blank }]), provideHttpClient(), provideHttpClientTesting()],
    });
    if (signedIn) {
      TestBed.inject(AuthState).start(
        { accessToken: 't', expiresAt: new Date(Date.now() + 3_600_000).toISOString(), role: 'manager', lineIds: [] },
        'manager',
      );
    }
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows no navigation before sign-in', () => {
    const element = render(false);

    expect(element.querySelector('nav')).toBeNull();
  });

  it('shows the product name and links to every section once signed in', () => {
    const element = render(true);
    const hrefs = Array.from(element.querySelectorAll('nav a')).map((a) => a.getAttribute('href'));

    expect(element.textContent).toContain(UK.appName);
    expect(hrefs).toEqual(['/', '/plan', '/fpy', '/defects']);
  });

  it('shows who is signed in and signs out on request', () => {
    const element = render(true);
    expect(element.textContent).toContain('manager');
    expect(element.textContent).toContain(UK.roles.manager);

    (element.querySelector('.account button') as HTMLButtonElement).click();

    expect(TestBed.inject(AuthState).isAuthenticated()).toBe(false);
  });
});
