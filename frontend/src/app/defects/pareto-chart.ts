import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import type { EChartsCoreOption } from 'echarts/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { AXIS_LABEL, CHART, SPLIT_LINE } from '../shared/charts/chart-theme';
import { formatPercent } from '../shared/format';
import { UK } from '../shared/i18n/uk';
import { ParetoItemDto } from '../shared/models/defects.dto';

interface TooltipParam {
  axisValue: string;
  marker: string;
  seriesName: string;
  value: number;
}

/** Defect counts (bars) with the cumulative share (line, right axis) and an 80% reference line. */
@Component({
  selector: 'app-pareto-chart',
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
export class ParetoChart {
  readonly items = input.required<ParetoItemDto[]>();
  readonly height = input(300);

  protected readonly ariaLabel = computed(() => {
    const top = this.items()
      .slice(0, 2)
      .map((i) => `${i.name} ${formatPercent(i.share, 0)}`)
      .join(', ');
    return `${UK.defects.pareto}: ${top}`;
  });

  protected readonly options = computed<EChartsCoreOption>(() => {
    const items = this.items();
    const names = new Map(items.map((i) => [i.code, i.name]));
    return {
      color: [CHART.blue, CHART.ink],
      grid: { left: 48, right: 52, top: 40, bottom: 28 },
      legend: { top: 0, left: 0, itemWidth: 14, itemHeight: 10, textStyle: { color: CHART.ink2 } },
      tooltip: {
        trigger: 'axis',
        axisPointer: { type: 'shadow' },
        formatter: (raw: unknown) => {
          const params = raw as TooltipParam[];
          const rows = params.map((p) => `${p.marker} ${p.seriesName}: <b>${p.value}</b>`);
          return `${params[0]?.axisValue} · ${names.get(params[0]?.axisValue) ?? ''}<br/>${rows.join('<br/>')}`;
        },
      },
      xAxis: {
        type: 'category',
        data: items.map((i) => i.code),
        axisLabel: { ...AXIS_LABEL, fontWeight: 600 },
        axisLine: { lineStyle: { color: CHART.line } },
        axisTick: { show: false },
      },
      yAxis: [
        { type: 'value', axisLabel: AXIS_LABEL, splitLine: SPLIT_LINE },
        {
          type: 'value',
          position: 'right',
          min: 0,
          max: 100,
          axisLabel: { ...AXIS_LABEL, formatter: '{value}%' },
          splitLine: { show: false },
        },
      ],
      series: [
        {
          name: UK.defects.legendCount,
          type: 'bar',
          data: items.map((i) => i.count),
          barMaxWidth: 40,
          label: { show: true, position: 'top', color: CHART.ink, fontWeight: 600 },
        },
        {
          name: UK.defects.legendCumulative,
          type: 'line',
          yAxisIndex: 1,
          data: items.map((i) => Math.round(i.cumulativeShare * 10) / 10),
          symbol: 'circle',
          symbolSize: 8,
          lineStyle: { width: 2.5 },
          markLine: {
            silent: true,
            symbol: 'none',
            lineStyle: { type: 'dashed', color: CHART.muted },
            label: { formatter: '80%', position: 'insideStartTop', color: CHART.muted },
            data: [{ yAxis: 80 }],
          },
        },
      ],
    };
  });
}
