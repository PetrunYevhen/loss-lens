import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import type { EChartsCoreOption } from 'echarts/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { AXIS_LABEL, CHART, SPLIT_LINE } from '../shared/charts/chart-theme';
import { formatDate, formatPercent } from '../shared/format';
import { UK } from '../shared/i18n/uk';
import { FpyTrendPointDto } from '../shared/models/fpy.dto';

/** Daily line RTY with the target as a dashed reference line. */
@Component({
  selector: 'app-fpy-trend-chart',
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
export class FpyTrendChart {
  readonly points = input.required<FpyTrendPointDto[]>();
  /** Target RTY in percent. */
  readonly target = input(92);
  readonly height = input(280);

  protected readonly ariaLabel = computed(() => {
    const last = this.points().at(-1);
    return `${UK.fpy.trend}: ${formatPercent(last?.rty)}`;
  });

  protected readonly options = computed<EChartsCoreOption>(() => {
    const points = this.points();
    const target = this.target();
    const values = points.map((p) => p.rty).filter((v): v is number => v !== null);
    return {
      color: [CHART.blue],
      grid: { left: 52, right: 24, top: 24, bottom: 28 },
      tooltip: {
        trigger: 'axis',
        valueFormatter: (value: unknown) => formatPercent(typeof value === 'number' ? value : null),
      },
      xAxis: {
        type: 'category',
        data: points.map((p) => formatDate(p.date).slice(0, 5)),
        axisLabel: { ...AXIS_LABEL, interval: Math.max(0, Math.ceil(points.length / 8) - 1) },
        axisLine: { lineStyle: { color: CHART.line } },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        min: values.length ? Math.floor(Math.min(...values, target) - 1) : 0,
        max: values.length ? Math.ceil(Math.max(...values, target) + 1) : 100,
        axisLabel: { ...AXIS_LABEL, formatter: '{value}%' },
        splitLine: SPLIT_LINE,
      },
      series: [
        {
          name: UK.kpi.lineRty,
          type: 'line',
          data: points.map((p) => (p.rty === null ? null : Math.round(p.rty * 100) / 100)),
          symbol: 'circle',
          symbolSize: 6,
          lineStyle: { width: 2.5 },
          markLine: {
            silent: true,
            symbol: 'none',
            lineStyle: { type: 'dashed', color: CHART.good, width: 1.5 },
            label: {
              formatter: `${UK.fpy.target} ${target}%`,
              position: 'insideEndTop',
              color: CHART.good,
              fontWeight: 600,
            },
            data: [{ yAxis: target }],
          },
        },
      ],
    };
  });
}
