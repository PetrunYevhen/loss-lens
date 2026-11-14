import { computed, effect, inject, Injectable, signal, Signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Params, Router } from '@angular/router';
import { filter } from 'rxjs';
import { isIsoDate, todayInKyiv } from '../dates';
import { ShiftId } from '../models/line.dto';

const ALL = 'all';
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
 * Default values are left out of the URL.
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
  readonly lineId = computed(() => this.text('line') ?? ALL);
  readonly shiftId = computed<ShiftId | typeof ALL>(() => {
    const value = this.params()['shift'];
    return value === 'A' || value === 'B' || value === 'C' ? value : ALL;
  });
  readonly stationId = computed(() => this.text('station') ?? ALL);
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
      queryParams['line'] = patch.lineId === ALL ? null : patch.lineId;
    }
    if (patch.shiftId !== undefined) {
      queryParams['shift'] = patch.shiftId === ALL ? null : patch.shiftId;
    }
    if (patch.stationId !== undefined) {
      queryParams['station'] = patch.stationId === ALL ? null : patch.stationId;
    }
    if (patch.autoRefresh !== undefined) {
      queryParams['auto'] = patch.autoRefresh ? '1' : null;
    }
    return this.router.navigate([], { queryParams, queryParamsHandling: 'merge' });
  }

  private text(name: string): string | null {
    const value = this.params()[name];
    return typeof value === 'string' && value !== '' ? value : null;
  }

  private readParams(): Params {
    return this.router.parseUrl(this.router.url).queryParams;
  }
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
