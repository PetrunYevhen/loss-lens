import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { formatPercent } from '../shared/format';
import { UK } from '../shared/i18n/uk';
import { StationFpyDto } from '../shared/models/fpy.dto';
import { ThresholdDto } from '../shared/models/thresholds.dto';
import { StatusBadge } from '../shared/ui/status-badge/status-badge';

const SCALE_MIN = 90;
const SCALE_MAX = 100;

/** Position on the 90-100% scale as a percentage of the track width. */
function position(value: number): number {
  const clamped = Math.min(SCALE_MAX, Math.max(SCALE_MIN, value));
  return ((clamped - SCALE_MIN) / (SCALE_MAX - SCALE_MIN)) * 100;
}

/**
 * FPY per station on a 90-100% scale with threshold marks. Each row links to the defects view
 * for that station. Status is shown with icon and text as well as color.
 */
@Component({
  selector: 'app-fpy-station-chart',
  imports: [RouterLink, StatusBadge],
  templateUrl: './fpy-station-chart.html',
  styleUrl: './fpy-station-chart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FpyStationChart {
  readonly stations = input.required<StationFpyDto[]>();
  readonly thresholds = input<ThresholdDto | null>(null);

  protected readonly t = UK.fpy;
  protected readonly format = formatPercent;

  protected readonly marks = computed(() => {
    const thresholds = this.thresholds();
    return thresholds
      ? [thresholds.amber, thresholds.green].map((value) => ({ value, left: position(value) }))
      : [];
  });

  protected readonly hint = computed(() => {
    const thresholds = this.thresholds();
    return thresholds ? `${this.t.scale} ${thresholds.amber}% і ${thresholds.green}%.` : '';
  });

  protected readonly rows = computed(() =>
    this.stations().map((station) => ({
      ...station,
      left: station.fpy === null ? null : position(station.fpy),
    })),
  );
}
