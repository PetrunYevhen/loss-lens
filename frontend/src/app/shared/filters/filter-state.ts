import { computed, effect, inject, Injectable, signal, Signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Params, Router } from '@angular/router';
import { filter } from 'rxjs';
import { isIsoDate, todayInKyiv } from '../dates';

const ALL = 'all';
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const AUTO_REFRESH_MS = 60_000;

export interface FilterPatch {
  date?: string;
  lineId?: string;
  shiftId?: string;
  stationId?: string;
  autoRefresh?: boolean;
}

/**
 * Date, line, shift and station filters. The URL query string is the single source of truth,
 * so any view can be shared and reopened (?date=...&line=...&shift=...&station=...&auto=1).
 * Default values are left out of the URL. Line, shift and station ids must be Guids: anything else in a
 * hand-edited or old URL counts as "all", so the page never asks the API a question it cannot answer.
 */
@Injectable({ providedIn: 'root' })
export class FilterState {
  private readonly router = inject(Router);
  private readonly params = signal<Params>(this.readParams());

  /** Bumps every 60 s while auto-refresh is on. Pages reload their data on each tick. */
  readonly tick = signal(0);

  readonly date = computed(() => {
    const value = this.params()['date'];
    return isIsoDate(value) ? value : todayInKyiv();
  });
  readonly lineId = computed(() => this.id('line'));
  readonly shiftId = computed(() => this.id('shift'));
  readonly stationId = computed(() => this.id('station'));
  readonly autoRefresh = computed(() => this.params()['auto'] === '1');

  /** Filter values as API parameters: `undefined` means "all". */
  readonly lineParam = computed(() => (this.lineId() === ALL ? undefined : this.lineId()));
  readonly shiftParam = computed(() => (this.shiftId() === ALL ? undefined : this.shiftId()));
  readonly stationParam = computed(() => (this.stationId() === ALL ? undefined : this.stationId()));

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.params.set(this.readParams()));

    effect((onCleanup) => {
      if (!this.autoRefresh()) {
        return;
      }
      const id = setInterval(() => this.tick.update((n) => n + 1), AUTO_REFRESH_MS);
      onCleanup(() => clearInterval(id));
    });
  }

  update(patch: FilterPatch): Promise<boolean> {
    const queryParams: Params = {};
    if (patch.date !== undefined) {
      queryParams['date'] =
        isIsoDate(patch.date) && patch.date !== todayInKyiv() ? patch.date : null;
    }
    if (patch.lineId !== undefined) {
      queryParams['line'] = idOrNull(patch.lineId);
      // A station belongs to one line, so a station from the previous line must not stay selected.
      if (patch.stationId === undefined) {
        queryParams['station'] = null;
      }
    }
    if (patch.shiftId !== undefined) {
      queryParams['shift'] = idOrNull(patch.shiftId);
    }
    if (patch.stationId !== undefined) {
      queryParams['station'] = idOrNull(patch.stationId);
    }
    if (patch.autoRefresh !== undefined) {
      queryParams['auto'] = patch.autoRefresh ? '1' : null;
    }
    return this.router.navigate([], { queryParams, queryParamsHandling: 'merge' });
  }

  private id(name: string): string {
    const value = this.params()[name];
    return typeof value === 'string' && GUID.test(value) ? value : ALL;
  }

  private readParams(): Params {
    return this.router.parseUrl(this.router.url).queryParams;
  }
}

/** The value for the URL: a Guid is kept, "all" and anything that is not a Guid is left out. */
function idOrNull(value: string): string | null {
  return GUID.test(value) ? value : null;
}

/** Reloads a resource on every auto-refresh tick. Call from an injection context. */
export function reloadOnTick(resource: { reload(): boolean }, tick: Signal<number>): void {
  let first = true;
  effect(() => {
    tick();
    if (first) {
      first = false;
      return;
    }
    untracked(() => resource.reload());
  });
}
