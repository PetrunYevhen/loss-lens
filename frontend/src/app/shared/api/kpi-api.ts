import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { KpiSummaryDto } from '../models/kpi.dto';
import { API_BASE } from './api-config';
import { toParams } from './http-params';

@Injectable({ providedIn: 'root' })
export class KpiApi {
  private readonly http = inject(HttpClient);

  getSummary(date: string, lineId?: string, shiftId?: string): Observable<KpiSummaryDto> {
    return this.http.get<KpiSummaryDto>(`${API_BASE}/kpi/summary`, {
      params: toParams({ date, lineId, shiftId }),
    });
  }
}
