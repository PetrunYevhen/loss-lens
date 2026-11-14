import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { todayInKyiv } from '../dates';
import { FilterState } from './filter-state';

@Component({ template: '' })
class Blank {}

describe('FilterState', () => {
  let router: Router;
  let filters: FilterState;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: '**', component: Blank }])],
    });
    router = TestBed.inject(Router);
    filters = TestBed.inject(FilterState);
  });

  it('uses defaults when the URL has no filters', async () => {
    await router.navigateByUrl('/fpy');

    expect(filters.date()).toBe(todayInKyiv());
    expect(filters.lineId()).toBe('all');
    expect(filters.shiftId()).toBe('all');
    expect(filters.stationId()).toBe('all');
    expect(filters.autoRefresh()).toBe(false);
    expect(filters.lineParam()).toBeUndefined();
  });

  it('writes changed filters to the URL and keeps the path', async () => {
    await router.navigateByUrl('/fpy');

    await filters.update({ lineId: 'L2', shiftId: 'B' });

    expect(router.url).toBe('/fpy?line=L2&shift=B');
    expect(filters.lineParam()).toBe('L2');
    expect(filters.shiftParam()).toBe('B');
  });

  it('leaves default values out of the URL', async () => {
    await router.navigateByUrl('/plan?line=L1&shift=A');

    await filters.update({ lineId: 'all', shiftId: 'all' });

    expect(router.url).toBe('/plan');
  });

  it('restores the same view from a shared URL', async () => {
    await router.navigateByUrl('/defects?date=2026-11-10&line=L3&shift=C&station=ICT&auto=1');

    expect(filters.date()).toBe('2026-11-10');
    expect(filters.lineId()).toBe('L3');
    expect(filters.shiftId()).toBe('C');
    expect(filters.stationId()).toBe('ICT');
    expect(filters.autoRefresh()).toBe(true);
  });

  it('ignores malformed values', async () => {
    await router.navigateByUrl('/plan?date=13-11-2026&shift=Z');

    expect(filters.date()).toBe(todayInKyiv());
    expect(filters.shiftId()).toBe('all');
  });

  it('keeps a non-default date in the URL and drops today', async () => {
    await router.navigateByUrl('/plan');

    await filters.update({ date: '2026-01-05' });
    expect(router.url).toBe('/plan?date=2026-01-05');

    await filters.update({ date: todayInKyiv() });
    expect(router.url).toBe('/plan');
  });
});
