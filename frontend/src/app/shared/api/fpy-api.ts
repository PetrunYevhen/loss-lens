import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { FpyTrendPointDto, StationFpyDto } from '../models/fpy.dto';
import { API_BASE } from './api-config';
import { toParams } from './http-params';

@Injectable({ providedIn: 'root' })
export class FpyApi {
  private readonly http = inject(HttpClient);

  getTrend(from: string, to: string, lineId?: string): Observable<FpyTrendPointDto[]> {
    return this.http.get<FpyTrendPointDto[]>(`${API_BASE}/fpy/trend`, {
      params: toParams({ from, to, lineId }),
    });
  }

  getStations(date: string, lineId?: string, shiftId?: string): Observable<StationFpyDto[]> {
    return this.http.get<StationFpyDto[]>(`${API_BASE}/fpy/stations`, {
      params: toParams({ date, lineId, shiftId }),
    });
  }
}
