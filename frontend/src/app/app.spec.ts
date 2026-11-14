import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { UK } from './shared/i18n/uk';

describe('App', () => {
  function render(): HTMLElement {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the product name and the demo data note', () => {
    const element = render();

    expect(element.textContent).toContain(UK.appName);
    expect(element.textContent).toContain(UK.demoNote.title);
  });

  it('links to every section', () => {
    const hrefs = Array.from(render().querySelectorAll('nav a')).map((a) => a.getAttribute('href'));

    expect(hrefs).toEqual(['/', '/plan', '/fpy', '/defects', '/roles']);
  });
});
