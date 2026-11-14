import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { ThresholdsDto } from '../models/thresholds.dto';
import { API_BASE } from './api-config';

@Injectable({ providedIn: 'root' })
export class ConfigApi {
  private readonly http = inject(HttpClient);

  getThresholds(): Observable<ThresholdsDto> {
    return this.http.get<ThresholdsDto>(`${API_BASE}/config/thresholds`);
  }
}

/** Thresholds are loaded once, they only change with the server configuration. */
@Injectable({ providedIn: 'root' })
export class ThresholdsState {
  private readonly api = inject(ConfigApi);

  readonly resource = rxResource({ stream: () => this.api.getThresholds() });
  readonly value = computed(() => this.resource.value() ?? null);
}
