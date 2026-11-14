import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { UK } from '../../shared/i18n/uk';
import { KpiCard } from './kpi-card';

describe('KpiCard', () => {
  function render(inputs: Record<string, unknown>): HTMLElement {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(KpiCard);
    fixture.componentRef.setInput('label', 'Виконання плану');
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the value with one decimal and the unit', () => {
    const element = render({ value: 90.46, unit: '%' });

    expect(element.querySelector('.kpi__number')?.textContent).toBe('90,5');
    expect(element.querySelector('.kpi__unit')?.textContent).toBe('%');
  });

  it('shows a dash and no unit when the value is null', () => {
    const element = render({ value: null, unit: '%' });

    expect(element.querySelector('.kpi__number')?.textContent).toBe('—');
    expect(element.querySelector('.kpi__unit')).toBeNull();
  });

  it('shows the status as text, not only as color', () => {
    const element = render({ value: 90, status: 'amber' });

    expect(element.textContent).toContain(UK.status.amber);
  });

  it('shows a text value instead of a number', () => {
    const element = render({ text: 'SB' });

    expect(element.querySelector('.kpi__number')?.textContent).toBe('SB');
  });

  it('marks a rise as good when higher is better', () => {
    const element = render({ value: 90, delta: 0.4, deltaUnit: UK.kpi.pp });

    const delta = element.querySelector('.delta');
    expect(delta?.classList).toContain('delta--good');
    expect(delta?.textContent).toContain('+0,4');
  });

  it('marks a rise as bad when lower is better', () => {
    const element = render({ value: 120, delta: 10, digits: 0, higherIsBetter: false });

    expect(element.querySelector('.delta')?.classList).toContain('delta--bad');
  });

  it('hides the change line when there is no delta', () => {
    expect(render({ value: 90 }).querySelector('.delta')).toBeNull();
  });

  it('renders a link that keeps the filters when a route is given', () => {
    const element = render({ value: 90, link: '/plan' });

    expect(element.querySelector('a.kpi')?.getAttribute('href')).toBe('/plan');
  });
});
