import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { LinesState } from '../api/lines-api';
import { ShiftsState } from '../api/shifts-api';
import { formatDate } from '../format';
import { UK } from '../i18n/uk';
import { AuthState } from '../auth/auth-state';
import { FilterState } from './filter-state';

@Component({
  selector: 'app-filter-bar',
  templateUrl: './filter-bar.html',
  styleUrl: './filter-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilterBar {
  readonly title = input.required<string>();
  readonly description = input('');

  protected readonly t = UK;
  protected readonly filters = inject(FilterState);
  protected readonly lines = inject(LinesState).list;
  protected readonly shifts = inject(ShiftsState).list;
  private readonly auth = inject(AuthState);

  protected readonly roleLabel = computed(() => {
    const role = this.auth.role();
    return role ? UK.roles[role] : '';
  });
  /**
   * The only line the user may see (an operator, or a supervisor with one line). There is nothing to choose,
   * so the bar shows the line itself instead of a list that starts with "all lines".
   */
  protected readonly onlyLine = computed(() => {
    const lines = this.lines();
    return lines.length === 1 ? lines[0] : null;
  });
  protected readonly scope = computed(() => {
    const lineId = this.filters.lineId();
    const line =
      this.onlyLine()?.name ??
      (lineId === 'all'
        ? UK.filters.allLines
        : (this.lines().find((l) => l.id === lineId)?.name ?? lineId));
    const description = this.description();
    return `${description ? description + ' · ' : ''}${line} · ${formatDate(this.filters.date())}`;
  });
}
