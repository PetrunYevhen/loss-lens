import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { UK } from '../../i18n/uk';
import { RagStatus } from '../../models/rag-status.dto';

/** Status is never conveyed by color alone: icon and text are always shown. */
@Component({
  selector: 'app-status-badge',
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': '"badge badge--" + status()' },
})
export class StatusBadge {
  readonly status = input.required<RagStatus>();
  protected readonly labels = UK.status;
}
