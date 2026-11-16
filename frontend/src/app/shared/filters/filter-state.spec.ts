import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { todayInKyiv } from '../dates';
import { FilterState } from './filter-state';

@Component({ template: '' })
class Blank {}

const LINE_1 = '11111111-1111-4111-8111-111111111111';
const LINE_2 = '22222222-2222-4222-8222-222222222222';
const LINE_3 = '33333333-3333-4333-8333-333333333333';
const SHIFT_B = '44444444-4444-4444-8444-444444444444';
const SHIFT_C = '55555555-5555-4555-8555-555555555555';
const STATION_1 = '66666666-6666-4666-8666-666666666666';
const STATION_9 = '99999999-9999-4999-8999-999999999999';

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

    await filters.update({ lineId: LINE_2, shiftId: SHIFT_B });

    expect(router.url).toBe(`/fpy?line=${LINE_2}&shift=${SHIFT_B}`);
    expect(filters.lineParam()).toBe(LINE_2);
    expect(filters.shiftParam()).toBe(SHIFT_B);
  });

  it('leaves default values out of the URL', async () => {
    await router.navigateByUrl(`/plan?line=${LINE_1}&shift=${SHIFT_B}`);

    await filters.update({ lineId: 'all', shiftId: 'all' });

    expect(router.url).toBe('/plan');
  });

  it('restores the same view from a shared URL', async () => {
    await router.navigateByUrl(
      `/defects?date=2026-11-10&line=${LINE_3}&shift=${SHIFT_C}&station=${STATION_1}&auto=1`,
    );

    expect(filters.date()).toBe('2026-11-10');
    expect(filters.lineId()).toBe(LINE_3);
    expect(filters.shiftId()).toBe(SHIFT_C);
    expect(filters.stationId()).toBe(STATION_1);
    expect(filters.autoRefresh()).toBe(true);
  });

  it('ignores a malformed date', async () => {
    await router.navigateByUrl('/plan?date=13-11-2026');

    expect(filters.date()).toBe(todayInKyiv());
  });

  it('takes a Guid shift id from the URL as it is', async () => {
    await router.navigateByUrl(`/plan?shift=${SHIFT_C}`);

    expect(filters.shiftId()).toBe(SHIFT_C);
    expect(filters.shiftParam()).toBe(SHIFT_C);
  });

  it('treats an id that is not a Guid as "all", so the API is never asked about it', async () => {
    await router.navigateByUrl('/plan?line=not-a-guid&shift=C&station=ICT');

    expect(filters.lineId()).toBe('all');
    expect(filters.shiftId()).toBe('all');
    expect(filters.stationId()).toBe('all');
    expect(filters.lineParam()).toBeUndefined();
    expect(filters.shiftParam()).toBeUndefined();
    expect(filters.stationParam()).toBeUndefined();
  });

  it('does not write an id that is not a Guid to the URL', async () => {
    await router.navigateByUrl('/plan');

    await filters.update({ lineId: 'L2', shiftId: 'B', stationId: 'ICT' });

    expect(router.url).toBe('/plan');
  });

  it('clears the station when the line changes, because a station belongs to one line', async () => {
    await router.navigateByUrl(`/defects?line=${LINE_1}&station=${STATION_1}`);

    await filters.update({ lineId: LINE_2 });

    expect(router.url).toBe(`/defects?line=${LINE_2}`);
    expect(filters.stationId()).toBe('all');
  });

  it('keeps a station that is set together with the line', async () => {
    await router.navigateByUrl('/defects');

    await filters.update({ lineId: LINE_2, stationId: STATION_9 });

    expect(router.url).toBe(`/defects?line=${LINE_2}&station=${STATION_9}`);
  });

  it('keeps a non-default date in the URL and drops today', async () => {
    await router.navigateByUrl('/plan');

    await filters.update({ date: '2026-01-05' });
    expect(router.url).toBe('/plan?date=2026-01-05');

    await filters.update({ date: todayInKyiv() });
    expect(router.url).toBe('/plan');
  });
});
