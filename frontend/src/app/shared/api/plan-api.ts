import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PlanVsActualPointDto } from '../models/plan.dto';
import { API_BASE } from './api-config';
import { toParams } from './http-params';

@Injectable({ providedIn: 'root' })
export class PlanApi {
  private readonly http = inject(HttpClient);

  getPlanVsActual(
    date: string,
    lineId?: string,
    shiftId?: string,
  ): Observable<PlanVsActualPointDto[]> {
    return this.http.get<PlanVsActualPointDto[]>(`${API_BASE}/plan-vs-actual`, {
      params: toParams({ date, lineId, shiftId }),
    });
  }
}
