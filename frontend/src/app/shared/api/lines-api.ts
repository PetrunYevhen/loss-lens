import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { LineDto } from '../models/line.dto';
import { AuthState } from '../auth/auth-state';
import { API_BASE } from './api-config';

@Injectable({ providedIn: 'root' })
export class LinesApi {
  private readonly http = inject(HttpClient);

  /** Lines the current role may see. */
  getLines(): Observable<LineDto[]> {
    return this.http.get<LineDto[]>(`${API_BASE}/lines`);
  }

}

/** Lines visible to the signed-in user, reloaded when another user signs in. */
@Injectable({ providedIn: 'root' })
export class LinesState {
  private readonly api = inject(LinesApi);
  private readonly auth = inject(AuthState);

  readonly resource = rxResource({
    // No user, no request: the resource stays idle until someone signs in.
    params: () => this.auth.username() ?? undefined,
    stream: () => this.api.getLines(),
  });

  readonly list = computed(() => this.resource.value() ?? []);
}
