import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { formatDelta, formatNumber } from '../../shared/format';
import { UK } from '../../shared/i18n/uk';
import { RagStatus } from '../../shared/models/rag-status.dto';
import { StatusBadge } from '../../shared/ui/status-badge/status-badge';

type DeltaTone = 'good' | 'bad' | 'neutral';

@Component({
  selector: 'app-kpi-card',
  imports: [NgTemplateOutlet, RouterLink, StatusBadge],
  templateUrl: './kpi-card.html',
  styleUrl: './kpi-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KpiCard {
  readonly label = input.required<string>();
  /** Numeric value. Null shows a dash. */
  readonly value = input<number | null>(null);
  /** Text value that replaces the number (for example a defect code). */
  readonly text = input<string | null>(null);
  readonly unit = input('');
  readonly digits = input(1);
  readonly caption = input('');
  readonly status = input<RagStatus | null>(null);
  /** Change versus the previous day. Null hides the change line. */
  readonly delta = input<number | null>(null);
  readonly deltaUnit = input('');
  readonly higherIsBetter = input(true);
  /** Route to open on click. Current filters are kept. */
  readonly link = input<string | null>(null);

  protected readonly vsPrevDay = UK.kpi.vsPrevDay;

  protected readonly display = computed(
    () => this.text() ?? formatNumber(this.value(), this.digits()),
  );
  protected readonly showUnit = computed(
    () => this.text() === null && this.value() !== null && this.unit() !== '',
  );

  protected readonly deltaText = computed(() => {
    const delta = this.delta();
    if (delta === null) {
      return null;
    }
    const unit = this.deltaUnit();
    return `${formatDelta(delta, this.digits())}${unit ? ' ' + unit : ''}`;
  });

  protected readonly deltaTone = computed<DeltaTone>(() => {
    const delta = this.delta();
    if (delta === null || delta === 0) {
      return 'neutral';
    }
    return delta > 0 === this.higherIsBetter() ? 'good' : 'bad';
  });

  protected readonly deltaUp = computed(() => (this.delta() ?? 0) >= 0);
}
