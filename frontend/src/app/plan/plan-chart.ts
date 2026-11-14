import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import type { EChartsCoreOption } from 'echarts/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { AXIS_LABEL, CHART, SPLIT_LINE } from '../shared/charts/chart-theme';
import { formatInt } from '../shared/format';
import { UK } from '../shared/i18n/uk';
import { PlanVsActualPointDto } from '../shared/models/plan.dto';

/** Hourly planned vs good units (bars) and cumulative good units (line, right axis). */
@Component({
  selector: 'app-plan-chart',
  imports: [NgxEchartsDirective],
  template: `<div
    echarts
    class="chart"
    role="img"
    [options]="options()"
    [style.height.px]="height()"
    [attr.aria-label]="ariaLabel()"
  ></div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanChart {
  readonly points = input.required<PlanVsActualPointDto[]>();
  readonly height = input(320);

  private readonly cumulative = computed(() => {
    let running = 0;
    return this.points().map((p) => (p.good === null ? null : (running += p.good)));
  });

  protected readonly ariaLabel = computed(() => {
    const total =
      this.cumulative()
        .filter((v): v is number => v !== null)
        .at(-1) ?? 0;
    return `${UK.plan.chart}: ${formatInt(total)} ${UK.kpi.pieces}`;
  });

  protected readonly options = computed<EChartsCoreOption>(() => {
    const points = this.points();
    return {
      color: [CHART.pale, CHART.blue, CHART.ink],
      grid: { left: 48, right: 56, top: 40, bottom: 28 },
      legend: { top: 0, left: 0, itemWidth: 14, itemHeight: 10, textStyle: { color: CHART.ink2 } },
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      xAxis: {
        type: 'category',
        data: points.map((p) => `${String(p.hour).padStart(2, '0')}:00`),
        axisLabel: { ...AXIS_LABEL, interval: points.length > 12 ? 2 : 0 },
        axisLine: { lineStyle: { color: CHART.line } },
        axisTick: { show: false },
      },
      yAxis: [
        { type: 'value', axisLabel: AXIS_LABEL, splitLine: SPLIT_LINE },
        { type: 'value', position: 'right', axisLabel: AXIS_LABEL, splitLine: { show: false } },
      ],
      series: [
        {
          name: UK.plan.legendPlanned,
          type: 'bar',
          data: points.map((p) => p.planned),
          barMaxWidth: 14,
        },
        {
          name: UK.plan.legendActual,
          type: 'bar',
          data: points.map((p) => p.good),
          barMaxWidth: 14,
        },
        {
          name: UK.plan.legendCumulative,
          type: 'line',
          yAxisIndex: 1,
          data: this.cumulative(),
          symbol: 'circle',
          symbolSize: 6,
          lineStyle: { width: 2.5 },
        },
      ],
    };
  });
}
