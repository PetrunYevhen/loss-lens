import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DefectsApi } from '../shared/api/defects-api';
import { addDays } from '../shared/dates';
import { FilterBar } from '../shared/filters/filter-bar';
import { FilterState, reloadOnTick } from '../shared/filters/filter-state';
import { UK } from '../shared/i18n/uk';
import { StationType } from '../shared/models/line.dto';
import { ViewState, viewStatus } from '../shared/ui/view-state/view-state';
import { KpiCard } from '../dashboard/kpi-card/kpi-card';
import { DefectTable } from './defect-table';
import { ParetoChart } from './pareto-chart';

@Component({
  selector: 'app-defects-page',
  imports: [FilterBar, KpiCard, ViewState, ParetoChart, DefectTable],
  templateUrl: './defects-page.html',
  styleUrl: './defects-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DefectsPage {
  protected readonly t = UK.defects;
  protected readonly kpi = UK.kpi;
  protected readonly filters = inject(FilterState);
  private readonly api = inject(DefectsApi);

  protected readonly stationTypes: readonly StationType[] = ['SPI', 'AOI', 'ICT', 'FCT'];
  protected readonly periods = [
    { days: 1, label: UK.defects.day },
    { days: 7, label: UK.defects.days7 },
    { days: 30, label: UK.defects.days30 },
  ] as const;
  protected readonly period = signal<number>(1);

  /** Station filter as a station type. Line-specific station ids ("L1-ICT") map to their type. */
  protected readonly activeStation = computed(() => {
    const id = this.filters.stationId();
    return id === 'all' ? 'all' : (id.split('-').pop() ?? 'all');
  });

  protected readonly pareto = rxResource({
    params: () => ({
      from: addDays(this.filters.date(), -(this.period() - 1)),
      to: this.filters.date(),
      lineId: this.filters.lineParam(),
      shiftId: this.filters.shiftParam(),
      stationId: this.filters.stationParam(),
    }),
    stream: ({ params }) => this.api.getPareto(params),
  });

  protected readonly state = computed(() => viewStatus(this.pareto, (r) => r.items.length === 0));
  protected readonly total = computed(() =>
    (this.pareto.value()?.items ?? []).reduce((sum, i) => sum + i.count, 0),
  );

  constructor() {
    reloadOnTick(this.pareto, this.filters.tick);
  }
}
