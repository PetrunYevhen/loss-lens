import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { PlanApi } from '../shared/api/plan-api';
import { FilterBar } from '../shared/filters/filter-bar';
import { FilterState, reloadOnTick } from '../shared/filters/filter-state';
import { formatHourRange, formatInt } from '../shared/format';
import { UK } from '../shared/i18n/uk';
import { ViewState, viewStatus } from '../shared/ui/view-state/view-state';
import { PlanChart } from './plan-chart';

interface HourRow {
  hour: number;
  label: string;
  planned: number;
  good: number | null;
  cumulative: number | null;
}

@Component({
  selector: 'app-plan-page',
  imports: [NgTemplateOutlet, FilterBar, ViewState, PlanChart],
  templateUrl: './plan-page.html',
  styleUrl: './plan-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanPage {
  protected readonly t = UK.plan;
  protected readonly filters = inject(FilterState);
  protected readonly int = formatInt;
  private readonly api = inject(PlanApi);

  protected readonly plan = rxResource({
    params: () => ({
      date: this.filters.date(),
      lineId: this.filters.lineParam(),
      shiftId: this.filters.shiftParam(),
    }),
    stream: ({ params }) => this.api.getPlanVsActual(params.date, params.lineId, params.shiftId),
  });

  protected readonly state = computed(() =>
    viewStatus(this.plan, (points) => points.every((p) => p.planned === 0)),
  );

  private readonly rows = computed<HourRow[]>(() => {
    let running = 0;
    return (this.plan.value() ?? []).map((point) => ({
      hour: point.hour,
      label: formatHourRange(point.hour),
      planned: point.planned,
      good: point.good,
      cumulative: point.good === null ? null : (running += point.good),
    }));
  });

  protected readonly firstHalf = computed(() => this.rows().filter((row) => row.hour < 12));
  protected readonly secondHalf = computed(() => this.rows().filter((row) => row.hour >= 12));

  constructor() {
    reloadOnTick(this.plan, this.filters.tick);
  }
}
