import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LinesState } from '../api/lines-api';
import { ShiftsState } from '../api/shifts-api';
import { LineDto } from '../models/line.dto';
import { FilterBar } from './filter-bar';

const line = (id: string, name: string): LineDto => ({ id, name, stations: [] });

function render(lines: LineDto[]): HTMLElement {
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      { provide: LinesState, useValue: { list: signal(lines) } },
      { provide: ShiftsState, useValue: { list: signal([]) } },
    ],
  });
  const fixture = TestBed.createComponent(FilterBar);
  fixture.componentRef.setInput('title', 'Дашборд');
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('FilterBar line filter', () => {
  it('shows the line itself, without a list, when the user has only one line', () => {
    const el = render([line('11111111-1111-1111-1111-111111111111', 'Лінія 1')]);

    expect(el.querySelector('.own-line')?.textContent).toContain('Лінія 1');
    expect(el.querySelector('select option[value="all"]')?.textContent).toContain('Усі зміни');
    expect(el.textContent).not.toContain('Усі лінії');
    expect(el.querySelector('.bar__scope')?.textContent).toContain('Лінія 1');
  });

  it('offers "all lines" and every line when the user has several', () => {
    const el = render([
      line('11111111-1111-1111-1111-111111111111', 'Лінія 1'),
      line('22222222-2222-2222-2222-222222222222', 'Лінія 2'),
    ]);

    expect(el.querySelector('.own-line')).toBeNull();
    expect(el.textContent).toContain('Усі лінії');
    expect(el.textContent).toContain('Лінія 2');
  });
});
