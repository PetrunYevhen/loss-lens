import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ParetoResponseDto } from '../models/defects.dto';
import { API_BASE } from './api-config';
import { toParams } from './http-params';

export interface ParetoQuery {
  from: string;
  to: string;
  lineId?: string;
  stationId?: string;
  shiftId?: string;
}

@Injectable({ providedIn: 'root' })
export class DefectsApi {
  private readonly http = inject(HttpClient);

  getPareto(query: ParetoQuery): Observable<ParetoResponseDto> {
    return this.http.get<ParetoResponseDto>(`${API_BASE}/defects/pareto`, {
      params: toParams({ ...query }),
    });
  }
}
