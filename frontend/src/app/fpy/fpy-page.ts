import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ThresholdsState } from '../shared/api/config-api';
import { FpyApi } from '../shared/api/fpy-api';
import { addDays } from '../shared/dates';
import { FilterBar } from '../shared/filters/filter-bar';
import { FilterState, reloadOnTick } from '../shared/filters/filter-state';
import { formatInt, formatPercent } from '../shared/format';
import { UK } from '../shared/i18n/uk';
import { StatusBadge } from '../shared/ui/status-badge/status-badge';
import { ViewState, viewStatus } from '../shared/ui/view-state/view-state';
import { FpyStationChart } from './fpy-station-chart';
import { FpyTrendChart } from './fpy-trend-chart';

@Component({
  selector: 'app-fpy-page',
  imports: [FilterBar, ViewState, StatusBadge, FpyStationChart, FpyTrendChart],
  templateUrl: './fpy-page.html',
  styleUrl: './fpy-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FpyPage {
  protected readonly t = UK.fpy;
  protected readonly filters = inject(FilterState);
  protected readonly thresholds = inject(ThresholdsState).value;
  protected readonly int = formatInt;
  protected readonly percent = formatPercent;
  private readonly api = inject(FpyApi);

  protected readonly periods = [7, 14, 30] as const;
  protected readonly days = signal<number>(30);

  protected readonly trend = rxResource({
    params: () => ({
      from: addDays(this.filters.date(), -(this.days() - 1)),
      to: this.filters.date(),
      lineId: this.filters.lineParam(),
    }),
    stream: ({ params }) => this.api.getTrend(params.from, params.to, params.lineId),
  });

  protected readonly stations = rxResource({
    params: () => ({
      date: this.filters.date(),
      lineId: this.filters.lineParam(),
      shiftId: this.filters.shiftParam(),
    }),
    stream: ({ params }) => this.api.getStations(params.date, params.lineId, params.shiftId),
  });

  protected readonly trendState = computed(() =>
    viewStatus(this.trend, (points) => points.every((p) => p.rty === null)),
  );
  protected readonly stationsState = computed(() =>
    viewStatus(this.stations, (rows) => rows.every((r) => r.tested === 0)),
  );
  protected readonly lineTarget = computed(() => this.thresholds()?.lineFpy.green ?? 92);

  constructor() {
    reloadOnTick(this.trend, this.filters.tick);
    reloadOnTick(this.stations, this.filters.tick);
  }
}
