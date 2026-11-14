import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ParetoChart } from '../defects/pareto-chart';
import { FpyStationChart } from '../fpy/fpy-station-chart';
import { PlanChart } from '../plan/plan-chart';
import { ThresholdsState } from '../shared/api/config-api';
import { DefectsApi } from '../shared/api/defects-api';
import { FpyApi } from '../shared/api/fpy-api';
import { KpiApi } from '../shared/api/kpi-api';
import { PlanApi } from '../shared/api/plan-api';
import { FilterBar } from '../shared/filters/filter-bar';
import { FilterState, reloadOnTick } from '../shared/filters/filter-state';
import { UK } from '../shared/i18n/uk';
import { ViewState, viewStatus } from '../shared/ui/view-state/view-state';
import { KpiCard } from './kpi-card/kpi-card';

@Component({
  selector: 'app-dashboard-page',
  imports: [FilterBar, KpiCard, ViewState, PlanChart, FpyStationChart, ParetoChart],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {
  protected readonly t = UK;
  protected readonly filters = inject(FilterState);
  protected readonly thresholds = inject(ThresholdsState).value;

  private readonly kpiApi = inject(KpiApi);
  private readonly planApi = inject(PlanApi);
  private readonly fpyApi = inject(FpyApi);
  private readonly defectsApi = inject(DefectsApi);

  private readonly scope = () => ({
    date: this.filters.date(),
    lineId: this.filters.lineParam(),
    shiftId: this.filters.shiftParam(),
  });

  protected readonly summary = rxResource({
    params: this.scope,
    stream: ({ params }) => this.kpiApi.getSummary(params.date, params.lineId, params.shiftId),
  });

  protected readonly plan = rxResource({
    params: this.scope,
    stream: ({ params }) =>
      this.planApi.getPlanVsActual(params.date, params.lineId, params.shiftId),
  });

  protected readonly stations = rxResource({
    params: this.scope,
    stream: ({ params }) => this.fpyApi.getStations(params.date, params.lineId, params.shiftId),
  });

  protected readonly pareto = rxResource({
    params: this.scope,
    stream: ({ params }) =>
      this.defectsApi.getPareto({
        from: params.date,
        to: params.date,
        lineId: params.lineId,
        shiftId: params.shiftId,
      }),
  });

  protected readonly summaryState = computed(() => viewStatus(this.summary));
  protected readonly planState = computed(() =>
    viewStatus(this.plan, (points) => points.every((p) => p.planned === 0)),
  );
  protected readonly stationsState = computed(() =>
    viewStatus(this.stations, (rows) => rows.every((r) => r.tested === 0)),
  );
  protected readonly paretoState = computed(() =>
    viewStatus(this.pareto, (r) => r.items.length === 0),
  );

  constructor() {
    for (const resource of [this.summary, this.plan, this.stations, this.pareto]) {
      reloadOnTick(resource, this.filters.tick);
    }
  }
}
