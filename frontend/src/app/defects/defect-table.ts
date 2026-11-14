import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { formatInt, formatPercent } from '../shared/format';
import { UK } from '../shared/i18n/uk';
import { ParetoItemDto } from '../shared/models/defects.dto';

@Component({
  selector: 'app-defect-table',
  template: `
    <div class="table-wrap">
      <table class="table">
        <caption class="sr-only">
          {{
            t.table
          }}
        </caption>
        <thead>
          <tr>
            <th scope="col">{{ t.code }}</th>
            <th scope="col">{{ t.name }}</th>
            <th scope="col" class="num">{{ t.count }}</th>
            <th scope="col" class="num">{{ t.share }}</th>
            <th scope="col" class="num">{{ t.cumulative }}</th>
          </tr>
        </thead>
        <tbody>
          @for (item of items(); track item.code) {
            <tr>
              <td>
                <strong>{{ item.code }}</strong>
              </td>
              <td>{{ item.name }}</td>
              <td class="num">{{ int(item.count) }}</td>
              <td class="num">{{ percent(item.share) }}</td>
              <td class="num">{{ percent(item.cumulativeShare) }}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DefectTable {
  readonly items = input.required<ParetoItemDto[]>();

  protected readonly t = UK.defects;
  protected readonly int = formatInt;
  protected readonly percent = formatPercent;
}
